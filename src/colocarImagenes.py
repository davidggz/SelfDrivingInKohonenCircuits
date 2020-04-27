import os
import shutil

DIRIMAGENES = "imagenesTotales"
OUTPUT = "inputAutoencoder"

dirs = os.listdir(DIRIMAGENES)

os.mkdir(OUTPUT)

imCounter = 0
for dirIm in dirs:
    imagenes = os.listdir(os.path.join(DIRIMAGENES, dirIm, 'input'))
    for im in imagenes:
        shutil.move(os.path.join(DIRIMAGENES, dirIm, 'input', im), os.path.join(OUTPUT, "img_" + str(imCounter).zfill(4) + ".png"))
        imCounter += 1