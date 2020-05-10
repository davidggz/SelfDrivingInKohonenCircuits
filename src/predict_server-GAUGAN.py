#!/usr/bin/env python
'''
Predict Server
Create a server to accept image inputs and run them against a trained neural network.
This then sends the steering output back to the client.
Author: Tawn Kramer
'''
from __future__ import print_function
import os
import argparse
import json
import time
import asyncore
import socket
from io import BytesIO
import base64
import datetime
import time

import numpy as np
from tensorflow.keras.models import load_model
from PIL import Image

from tcp_server import IMesgHandler, SimServer
import conf

import matplotlib.pyplot as plt
import cv2

import time

# Imports para Pix2PixHD
import sys
sys.path.insert(0, os.getcwd() + '/SPADE')

from SPADE.loadInference import load_GAUGAN
from SPADE.loadInference import infere_GAUGAN

from skimage.io import imread
import re
import tensorflow as tf

from keras.backend.tensorflow_backend import set_session
from keras.backend.tensorflow_backend import clear_session
import gc
import torch
from keras import backend as K

class DonkeySimMsgHandler(IMesgHandler):

    STEERING = 0
    THROTTLE = 1
    
    listaColores = np.uint8([[
        (0, 0, 0),           # unlabeled
        (111, 74,  0),      # dynamic
        ( 81,  0, 81),      # ground
        (128, 64, 128),     # Carretera
        (127, 64, 127),     # Carretera
        (244, 35, 232),     # Sidewalk
        (250,170,160),      # parking
        (230,150,140),      # rail trail
        ( 70, 70, 70),      # building
        (102,102,156),      # wall
        (190,153,153),      # fence
        (180,165,180),      # guard rail
        (150,100,100),      # bridge
        (150,120, 90),      # tunnel
        (153,153,153),      # pole
        (153,153,153),      # polegroup
        (250,170, 30),      # traffic light
        (220,220,  0),      # traffic sign
        (107, 142, 35),     # Vegetacion (arbol)
        (152, 251, 152),    # Cesped (terrain)
        (70, 130, 180),     # Cielo
        (220, 20, 60),      # Person
        (255,  0,  0),      # rider
        (  0, 0, 142),     # car
        (  0,  0, 70),      # truck
        (  0, 60, 100),     # bus
        (  0,  0, 90),      # caravan
        (  0,  0, 110),     # trailer
        (  0, 80, 100),     # train
        (  0,  0, 230),     # motorcycle
        (119, 11, 32),      # bicycle
    ]])

    listaLabels = [
        (0, 0, 0),      # unlabeled
        (5, 5, 5),      # dynamic
        (6, 6, 6),      # ground
        (7, 7, 7),      # Carretera
        (7, 7, 7),      # Carretera
        (8, 8, 8),      # Sidewalk
        (9, 9, 9),      # parking
        (10, 10, 10),   # rail trail
        (11, 11, 11),   # building
        (12, 12, 12),   # wall
        (13, 13, 13),   # fence
        (14, 14, 14),   # guard rail
        (15, 15, 15),   # bridge
        (16, 16, 16),   # tunnel
        (17, 17, 17),   # pole
        (18, 18, 18),   # pole group
        (19, 19, 19),   # traffic light
        (20, 20, 20),   # traffic sign
        (21, 21, 21),   # Vegetacion (arbol)
        (22, 22, 22),   # Cesped
        (23, 23, 23),   # Cielo
        (24, 24, 24),   # person
        (25, 25, 25),   # rider
        (26, 26, 26),   # car
        (27, 27, 27),   # truck
        (28, 28, 28),   # bus
        (29, 29, 29),   # caravan
        (30, 30, 30),   # trailer
        (31, 31, 31),   # train
        (32, 32, 32),   # motorcycle
        (33, 33, 33),   # bicycle
    ]

    def __init__(self, modelDict, constant_throttle, port=0, num_cars=1, image_cb=None, rand_seed=0):
        self.modelDict = modelDict
        self.constant_throttle = constant_throttle
        self.sock = None
        self.image_folder = None
        self.image_cb = image_cb
        self.steering_angle = 0.
        self.throttle = 0.
        self.num_cars = 0
        self.port = port
        self.target_num_cars = num_cars
        self.rand_seed = rand_seed
        self.fns = {'telemetry' : self.on_telemetry,\
                    'car_loaded' : self.on_car_created,\
                    'on_disconnect' : self.on_disconnect,\
                    'telemetryGAN' : self.on_telemetryGAN}

        self.listaColores = cv2.cvtColor(self.listaColores, cv2.COLOR_BGR2HSV)

    def on_connect(self, socketHandler):
        self.sock = socketHandler

    def on_disconnect(self):
        self.num_cars = 0

    def on_recv_message(self, message):
        if not 'msg_type' in message:
            print('expected msg_type field')
            return

        msg_type = message['msg_type']
        if msg_type in self.fns:
            self.fns[msg_type](message)
        else:
            print('unknown message type', msg_type)

    def recogerLineas(self, image):
        imgRGB = image.copy()
        imgPix2PixHD = imgRGB.copy()
        imgHSV = cv2.cvtColor(imgRGB, cv2.COLOR_RGB2HSV)

        # Eliminación del paisaje exceptuando la carretera y las lineas
        for index, color in enumerate(self.listaColores[0]):
            # Se pone como limite superior e inferior el mismo
            # color para que no coja otros que tengan el mismo HUE.
            lowerLimit = color[0], color[1], color[2] 
            upperLimit = color[0], color[1], color[2]

            mascara = cv2.inRange(imgHSV, np.uint8(lowerLimit), np.uint8(upperLimit))

            if index == 0:
                maskGlobal = mascara
            else:
                if self.listaLabels[index] != (7, 7, 7):
                    maskGlobal = cv2.bitwise_or(maskGlobal, mascara)

        imgRGB[maskGlobal > 0] = (0, 0, 0)
        #imgHSV = cv2.cvtColor(imgRGB, cv2.COLOR_RGB2HSV)

        # Generamos la imagen de entrada a Pix2PixHD
        imgPix2PixHD[maskGlobal <= 0] = (128, 64, 128)

        # Generamos la imagen con solo las lineas y su mascara correspondiente
        imgHSV = cv2.cvtColor(imgRGB, cv2.COLOR_RGB2HSV)

        # Se segmenta el gris
        lowerLimit = 0, 0, 150
        upperLimit = 142, 255, 255

        maskLine = cv2.inRange(imgHSV, np.uint8(lowerLimit), np.uint8(upperLimit))
        maskOnlyRoad = cv2.bitwise_not(maskLine)

        imgRGB[maskOnlyRoad > 0] = (0, 0, 0)

        return imgPix2PixHD, imgRGB, maskLine

    def introducirLinea(self, imgDestino, imgLineas, maskLineas):

        maskLineas = cv2.bitwise_not(maskLineas)
        entorno = cv2.bitwise_or(imgDestino, imgDestino, mask = maskLineas)
        
        montada = np.add(entorno, imgLineas)

        return montada

    def crearDirectoriosLog(self):
        if not os.path.exists('LogImages'):
            os.mkdir('./LogImages')
            os.mkdir('./LogImages/input')
            os.mkdir('./LogImages/output')

    def guardarImagenes(self, imgReal, imgLineas, counter):
        image_pil = Image.fromarray(imgReal)
        image_pil.save(os.path.join('./LogImages/input', "img_" + str(counter) +  ".png"))

        image_pil = Image.fromarray(imgLineas)
        image_pil.save(os.path.join('./LogImages/output', "img_" + str(counter) +  ".png"))

    def on_telemetryGAN(self, data):

        #fileEnvio = open("C:/Users/david/Documents/Projects/drivingSimulator/src/PIPE_ENVIO.txt", "r")
        #fileEntrega = open("C:/Users/david/Documents/Projects/drivingSimulator/src/PIPE_ENTREGA.txt", "w")
        enviado = False
        dirEnvio = os.path.join(os.getcwd(), "ENVIO")
        dirEntrega = os.path.join(os.getcwd(), "ENTREGA")
        contadorEnviadas = 0
        contadorImagenes = 0
        tamImagenes = (256, 512, 3)


        prueba = True

        #self.crearDirectoriosLog()
        while(True):
            
            imgNames = os.listdir(dirEnvio)
            outNames = os.listdir(dirEntrega)
            #and len(outNames) < 2
            if len(imgNames) > 1:
                '''
                if enviado == False:
                    imagen = cv2.imread("prueba.png")
                    self.infereControl(imagen)
                '''

                start = time.time()
                imgName = "ENVIO_" + str(contadorEnviadas) + ".png"
                '''
                # Funcionalidad necesaria si se quiere inferir con la red
                image = imread(os.path.join(dirEnvio, imgNames[0]))
                os.remove(os.path.join(dirEnvio, imgNames[0]))
                image_array = np.array(image)
                '''

                # Se lee la imagen 
                imagenEntrada = cv2.imread(os.path.join(dirEnvio, imgName))

                # PROCESAMIENTO POR SI ENTRAN SOLO LAS LINEAS DE LA CARRETERA
                seg_GAUGAN, imgLineas, maskLineas = self.recogerLineas(imagenEntrada)

                # PROCESAMIENTO DE IMAGEN
                # Si la carretera es "realista" tenemos que segmentarla.
                #imagenSeg, maskRoad = self.changeRoad(imagenEntrada)
                #cv2.imwrite("C:/Users/david/Desktop/mierda/entrada.png", imagenEntrada) 
                #cv2.imwrite("C:/Users/david/Desktop/mierda/mask.png", maskRoad) 

                # La generación de las labels puede hacerse con OpenCV o con la red convolucional
                # label = self.infere_label(image_array, tamImagenes)
                label = self.toLabel(seg_GAUGAN)
                with torch.no_grad():
                    generada = infere_GAUGAN(self.modelDict['ganModel'], label, tamImagenes)

                generada = self.introducirLinea(generada, imgLineas, maskLineas)

                #self.guardarImagenes(generada, imgLineas, contadorImagenes)
                contadorImagenes += 1

                '''
                # Codigo para montar la imagen procesada con la carretera de Unity
                entorno = cv2.bitwise_or(generada, generada, mask = maskRoad)
                maskRoad = cv2.bitwise_not(maskRoad)
                realRoad = cv2.bitwise_or(imagenEntrada, imagenEntrada, mask = maskRoad)
                generada = np.add(realRoad, entorno)
                '''

                # Se elimina la imagen recien procesada y se escribe el resultado del procesamiento.
                os.remove(os.path.join(dirEnvio, imgName))

                image_pil = Image.fromarray(generada)
                image_pil.save(os.path.join(dirEntrega, "ENTREGA_" + str(contadorEnviadas) +  ".jpg"))
            
                # Se cambia el contador para saber qué imagen cong
                if contadorEnviadas == 0:
                    contadorEnviadas = 1
                else:
                    contadorEnviadas = 0

                #generada = cv2.imread(os.path.join(os.getcwd(), "prueba.png"))

                steering = self.infereControl(generada)
                self.parseControl(steering)
                
                '''
                del generada
                del label
                gc.collect()
                torch.cuda.empty_cache()
                '''

                end = time.time()
                print("TIEMPO: " +  str(end - start))
                
                if enviado == False:
                    time.sleep(0.5)
                    self.send_GAN_message()
                    enviado = True

    def infereControl(self, imagen):

        imagen = imagen / 255.0
        imagen = np.reshape(imagen, (1, 256, 512, 3))

        steering = self.modelDict['steeringModel'].predict(imagen)
        
        print("STEERING: " + str(steering[0][0] * 25))
        return steering[0][0]

    def parseControl(self, steering_angle):
        self.steering_angle = steering_angle
        self.throttle = 0.2

        self.enviarControlGAN(self.steering_angle, self.throttle)

    def enviarControlGAN (self, steer, throttle):
        # Se envía aquí directamente el mensaje en vez de utilizar
        # las funciones del socket porque así se puede hacer el while(true)
        msg = { 'msg_type' : 'control', 'steering': steer.__str__(), 'throttle':throttle.__str__(), 'brake': '0.0' }
        json_msg = json.dumps(msg)
        data = json_msg.encode()
        sent = self.sock.send(data[:self.sock.chunk_size])
            
    def toLabel(self, imgRGB):
        imagen = imgRGB.copy()
        imgHSV = cv2.cvtColor(imagen, cv2.COLOR_RGB2HSV)

        for index, color in enumerate(self.listaColores[0]):
            lowerLimit = color
            upperLimit = color

            mascara = cv2.inRange(imgHSV, np.uint8(lowerLimit), np.uint8(upperLimit))

            imagen[mascara > 0] = self.listaLabels[index]

        return imagen

    def changeRoad(self, imgRGB):
        imagen = imgRGB.copy()
        imgHSV = cv2.cvtColor(imagen, cv2.COLOR_RGB2HSV)

        for index, color in enumerate(self.listaColores[0]):
            lowerLimit = color
            upperLimit = color

            mascara = cv2.inRange(imgHSV, np.uint8(lowerLimit), np.uint8(upperLimit))

            if index == 0:
                maskGlobal = mascara
            else:
                maskGlobal = cv2.bitwise_or(maskGlobal, mascara)

        imagen[maskGlobal <= 0] = (128, 64, 128)

        return imagen, maskGlobal

    def sorted_nicely(self, l ): 
        """ Sort the given iterable in the way that humans expect.""" 
        convert = lambda text: int(text) if text.isdigit() else text 
        alphanum_key = lambda key: [ convert(c) for c in re.split('([0-9]+)', key) ] 
        return sorted(l, key = alphanum_key)

    def infere_label(self, imgArray, tamImagen):
        # Se normaliza y se cambia el shape de la imagen para poder hacer inferencia.
        imgArray = imgArray / 255.0
        imgArray = np.reshape(imgArray, (1, tamImagen[0], tamImagen[1], tamImagen[2]))
        # Se infiere en el modelo del labeler
        label = self.modelDict['labelerModel'].predict(imgArray)
        # Los resultados están normalizados, así que multiplicamos, hacemos un redondeo
        # y casteamos todos los valores a int de 8 bits.
        label = label * 255
        label = np.around(label)
        label = label.astype(np.uint8)

        '''
        # PIL necesita al menos 3 canales para poder codificar como PNG, asi que 
        # con este codigo duplicamos el canal en 3 por si hay que utilizar la imagen.
        label = np.reshape(label, (tamImagen[0], tamImagen[1]))
        label = np.stack((label,)*3, axis=-1)
        '''

        return label

    def send_GAN_message(self):
        # Se envía aquí directamente el mansaje en vez de utilizar
        # las funciones del socket porque así se puede hacer el while(true)
        msg = { "msg_type" : "GANResult" }
        json_msg = json.dumps(msg)
        data = json_msg.encode()
        sent = self.sock.send(data[:self.sock.chunk_size])


        #self.sock.queue_message(msg)

    def on_car_created(self, data):
        if self.rand_seed != 0:
            self.send_regen_road(0, self.rand_seed, 1.0)

        self.num_cars += 1
        if self.num_cars < self.target_num_cars:
            print("requesting another car..")
            self.request_another_car()

    def on_telemetry(self, data):
        imgString = data["image"]
        image = Image.open(BytesIO(base64.b64decode(imgString)))
        image_array = np.asarray(image)
        self.predict(image_array)

        if self.image_cb is not None:
            self.image_cb(image_array, self.steering_angle )

        # maybe save frame
        if self.image_folder is not None:
            timestamp = datetime.utcnow().strftime('%Y_%m_%d_%H_%M_%S_%f')[:-3]
            image_filename = os.path.join(self.image_folder, timestamp)
            image.save('{}.jpg'.format(image_filename))


    def predict(self, image_array):
        outputs = self.modelDict['steeringModel'].predict(image_array[None, :, :, :])
        self.parse_outputs(outputs)
    
    def parse_outputs(self, outputs):
        res = []
        #print("Outputs")
        #print(outputs)
        for output in outputs:     
            #print("output")       
            #print(output)
            for i in range(output.shape[0]):
                res.append(output[i])
        #print("Res")
        #print(res)

        self.on_parsed_outputs(res)
        
    def on_parsed_outputs(self, outputs):
        self.outputs = outputs
        self.steering_angle = 0.0
        self.throttle = 0.2

        if len(outputs) > 0:        
            self.steering_angle = outputs[self.STEERING]

        if self.constant_throttle != 0.0:
            self.throttle = self.constant_throttle
        elif len(outputs) > 1:
            self.throttle = outputs[self.THROTTLE] * conf.throttle_out_scale

        self.send_control(self.steering_angle, self.throttle)

    def send_control(self, steer, throttle):
        msg = { 'msg_type' : 'control', 'steering': steer.__str__(), 'throttle':throttle.__str__(), 'brake': '0.0' }
        print(steer.__str__())
        self.sock.queue_message(msg)

    def send_regen_road(self, road_style=0, rand_seed=0, turn_increment=0.0):
        '''
        Regenerate the road, where available. For now only in level 0.
        In level 0 there are currently 5 road styles. This changes the texture on the road
        and also the road width.
        The rand_seed can be used to get some determinism in road generation.
        The turn_increment defaults to 1.0 internally. Provide a non zero positive float
        to affect the curviness of the road. Smaller numbers will provide more shallow curves.
        '''
        msg = { 'msg_type' : 'regen_road',
            'road_style': road_style.__str__(),
            'rand_seed': rand_seed.__str__(),
            'turn_increment': turn_increment.__str__() }
        
        self.sock.queue_message(msg)

    def request_another_car(self):
        port = self.port + self.num_cars
        address = ("0.0.0.0", port)
        
        #spawn a new message handler serving on the new port.
        handler = DonkeySimMsgHandler(self.model, 0., num_cars=(self.target_num_cars - 1), port=address[1])
        server = SimServer(address, handler)

        msg = { 'msg_type' : 'new_car', 'host': '127.0.0.1', 'port' : port.__str__() }
        self.sock.queue_message(msg)   

    def on_close(self):
        pass


def goAntiguo(filename, address, constant_throttle=0, num_cars=1, image_cb=None, rand_seed=None):

    # Carga de la red de neuronas o del modelo que sea
    model = load_model(filename)

    #looks like we have to compile it before use. These optimizers don't matter for inference.
    model.compile("sgd", "mse")
  
    #setup the server
    # Se crea el handler. El handler contiene todos los Callback dependiendo del mensaje que reciba.
    handler = DonkeySimMsgHandler(model, constant_throttle, port=address[1], num_cars=num_cars, image_cb=image_cb, rand_seed=rand_seed)
    server = SimServer(address, handler)

    try:
        #asyncore.loop() will keep looping as long as any asyncore dispatchers are alive
        asyncore.loop()
    except KeyboardInterrupt:
        #unless some hits Ctrl+C and then we get this interrupt
        print('stopping')

def go(modelosName, address, constant_throttle=0, num_cars=1, image_cb=None, rand_seed=None):

    steeringModel = None
    ganModel = None
    labelerModel = None
    '''
    config = tf.ConfigProto()
    #config.gpu_options.allow_growth = True  # dynamically grow the memory used on the GPU
    #config.log_device_placement = True  # to log device placement (on which device the operation ran)
    config.gpu_options.per_process_gpu_memory_fraction = 0.8
    sess = tf.Session(config=config)
    set_session(sess)  # set this TensorFlow session as the default session for Keras
    '''

    config = tf.ConfigProto()
    config.gpu_options.per_process_gpu_memory_fraction = 0.15
    #config.gpu_options.allow_growth = True
    set_session(tf.Session(config=config))

    # Carga de los modelos necesarios para hacer inferencia
    if modelosName['steeringModel'] != None:
        steeringModel = load_model(modelosName['steeringModel'])
        #steeringModel.compile("sgd", "mse")

        '''
        steeringModel = tf.keras.models.load_model(
            modelosName['steeringModel'],
            custom_objects=None,
            compile=False
        )
        '''

    if modelosName['GAN'] != None:
        os.chdir('./SPADE')
        ganModel = load_GAUGAN(modelosName['GAN'])
        #ganModel.compile("sgd", "mse")
        os.chdir('../')
        
    if modelosName['labeler'] != None:
        labelerModel = load_model('./labeler/' + modelosName['labeler'] + '.h5')
        #labelerModel.compile("sgd", "mse")

    modelDict = {'steeringModel': steeringModel, 'ganModel': ganModel, 'labelerModel': labelerModel}
  
    #setup the server
    # Se crea el handler. El handler contiene todos los Callback dependiendo del mensaje que reciba.
    handler = DonkeySimMsgHandler(modelDict, constant_throttle, port=address[1], num_cars=num_cars, image_cb=image_cb, rand_seed=rand_seed)
    server = SimServer(address, handler)

    try:
        #asyncore.loop() will keep looping as long as any asyncore dispatchers are alive
        asyncore.loop()
    except KeyboardInterrupt:
        #unless some hits Ctrl+C and then we get this interrupt
        print('stopping')

# ***** main loop *****
if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='prediction server')
    parser.add_argument('--steeringModel', type=str, help='model filename')
    # Directorio de los dos modelos necesarios para hacer la inferencia en Pix2PixHD
    parser.add_argument('--GAN', type=str, help='Modelo para generar imagenes realistas. Preparado para Pix2PixHD.')
    parser.add_argument('--labeler', type=str, help='Modelo para pasar a labels.')
    parser.add_argument('--host', type=str, default='0.0.0.0', help='bind to ip')
    parser.add_argument('--port', type=int, default=9090, help='bind to port')
    parser.add_argument('--num_cars', type=int, default=1, help='how many cars to spawn')
    parser.add_argument('--constant_throttle', type=float, default=1.0, help='apply constant throttle')
    parser.add_argument('--rand_seed', type=int, default=0, help='set road generation random seed')
    args = parser.parse_args()

    modelosName = {'steeringModel': args.steeringModel, 'GAN': args.GAN, 'labeler': args.labeler}
    #print(modelosName)

    address = (args.host, args.port)
    go(modelosName, address, args.constant_throttle, num_cars=args.num_cars, rand_seed=args.rand_seed)
