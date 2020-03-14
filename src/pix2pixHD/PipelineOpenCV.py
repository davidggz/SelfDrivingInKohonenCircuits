import os
from collections import OrderedDict
from torch.autograd import Variable
from options.test_options import TestOptions
from data.data_loader import CreateDataLoader
from models.models import create_model
import util.util as util
from util.visualizer import Visualizer
from util import html
import torch
import cv2

opt = TestOptions().parse(save=False)
opt.nThreads = 1   # test code only supports nThreads = 1
opt.batchSize = 1  # test code only supports batchSize = 1
opt.serial_batches = True  # no shuffle
opt.no_flip = True  # no flip

'''Options metidas por mi directamente'''
opt.name = "Mod1-OnlyCityscapes-512"
opt.results_dir = "./results/"
opt.which_epoch = "latest"
opt.netG = "local"
opt.ngf = 32
opt.resize_or_crop = None
opt.dataroot = "./datasets/cityscapes"
opt.how_many = 50000
opt.no_instance = True

'''Lista de colores con sus correspondientes labels'''
listaColores = np.uint8([[
    (129, 65, 129),     # Carretera
    (107, 142, 35),     # Arbol
    (152, 251, 152),    # Cesped
    (70, 130, 180)      # Cielo
]])

listaLabels = [
    (7, 7, 7),      # Carretera
    (21, 21, 21),   # Arbol
    (22, 22, 22),   # Cesped
    (23, 23, 23)    # Cielo
]

listaColores = cv2.cvtColor(listaColores, cv2.COLOR_BGR2HSV)

'''Data set de la inferencia'''
DIR_NAME = './datasetPipeline'

imgList = os.listdir(DIR_NAME)
if not os.path.exists("output"):
    os.mkdir("output")

def toLabel(path):
    imgRGB = cv2.imread(path)
    imgHSV = cv2.cvtColor(imgRGB, cv2.COLOR_RGB2HSV)

    for index, color in enumerate(listaColores[0]):
        lowerLimit = color[0] - 10, 0, 0
        upperLimit = color[0] + 10, 255, 255

        mascara = cv2.inRange(imgHSV, np.uint8(lowerLimit), np.uint8(upperLimit))

        imgRGB[mascara > 0] = listaLabels[index]

    return imgRGB

# Cargamos el modelo 
if not opt.engine and not opt.onnx:
    model = create_model(opt)
    if opt.data_type == 16:
        model.half()
    elif opt.data_type == 8:
        model.type(torch.uint8)
            
    if opt.verbose:
        print(model)
else:
    from run_engine import run_trt_engine, run_onnx

for imgName in imgList:
    label = toLabel(DIR_NAME + imgName)

    generated = model.inference(label, None, None)

    cv2.imwrite("generada.png", generated)

