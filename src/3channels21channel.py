import cv2
import os
import numpy as np
import matplotlib.pyplot as plt

DIR = 'outputAutoencoder'
im_ids = os.listdir(DIR)

lowerLimit = (0, 0, 0)
upperLimit = (0, 0, 0)

for im_id in im_ids:
    imgRGB = cv2.imread(os.path.join(DIR, im_id))
    imgHSV = cv2.cvtColor(imgRGB, cv2.COLOR_RGB2HSV)


    mascara = cv2.inRange(imgHSV, np.uint8(lowerLimit), np.uint8(upperLimit))
    mascara = cv2.bitwise_not(mascara)

    imgRGB[mascara > 0] = (255, 255, 255)

    cv2.imwrite(os.path.join(DIR, im_id), imgRGB)