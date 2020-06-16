import os
import cv2 as cv
from skimage import transform

imagen = "./Night_City.png"

imagen = cv.imread(imagen, cv.IMREAD_UNCHANGED)

imgResized = cv.resize(imagen, (512, 256))

cv.imwrite("Night_City_cambiada.png", imgResized)

