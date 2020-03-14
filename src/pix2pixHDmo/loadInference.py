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
import numpy as np
import time

from keras.models import load_model
from multi_gpu import to_multi_gpu
from skimage.io import imread
import numpy as np
import matplotlib.pyplot as plt
import cv2 as cv

opt = TestOptions().parse(save=False)
opt.nThreads = 1   # test code only supports nThreads = 1
opt.batchSize = 1  # test code only supports batchSize = 1
opt.serial_batches = True  # no shuffle
opt.no_flip = True  # no flip

def load_Pix2PixHD(nameModel):
    '''Options metidas por mi directamente'''
    opt.name = nameModel #"Mod1-OnlyCityscapes-512"
    
    opt.checkpoints_dir = './checkpoints'
    opt.results_dir = "./results/"
    opt.which_epoch = "latest"
    opt.netG = "local"
    opt.ngf = 32
    opt.resize_or_crop = None
    opt.dataroot = "./datasets/cityscapes"
    opt.how_many = 50000
    opt.no_instance = True
    opt.verbose = False

    # Cargamos el modelo 
    if not opt.engine and not opt.onnx:
        model = create_model(opt)
        if opt.data_type == 16:
            model.half()
        elif opt.data_type == 8:
            model.type(torch.uint8)
                
        if opt.verbose:
            print(model)

        return model
    else:
        from run_engine import run_trt_engine, run_onnx


def get_input(path):

    img = imread(path)
    img = np.array(img)
    img = img / 255.0
    img = np.reshape(img, (1, 256, 512, 3))

    return(img) 

def infere_Pix2PixHD(model, label, tamImagen):
    label = np.reshape(label, (1, 1, tamImagen[0], tamImagen[1]))
    label = torch.from_numpy(label)
    inst = torch.from_numpy(np.array([0]))

    generated = model.inference(label, inst, None)

    return util.tensor2im(generated.data[0])