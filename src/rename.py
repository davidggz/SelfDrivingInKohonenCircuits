import os

dirSeg = "./inputAutoencoder/"
dirLabels = "./outputAutoencoder/"

seg = os.listdir(dirSeg)
labels = os.listdir(dirLabels)
seg.sort()
labels.sort()

index = 3746
for segment, label in zip(seg, labels):

    os.rename(dirSeg + segment, dirSeg + "img_" + str(index).zfill(4) + ".png")
    os.rename(dirLabels + label, dirLabels + "img_" + str(index).zfill(4) + ".png")

    index += 1