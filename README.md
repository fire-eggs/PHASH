# PHASH
My implementation of "perceptual hash" (phash) for images.

To find duplicate / similar images is a two-phase process.

Phase 1:
Calculate the phash value for all images in a folder and sub-folders. The image paths and phashes are stored in a file.

Phase 2:
Load a file from phase 1 into a viewer. It compares all image phash values and shows a list of image pairs, ordered by phash simularity. Rows in the list are selected to view the two images side-by-side.

20151128:
Uploaded the code for phase 1. The initial check-in uses CImg to load files; due to various issues it is limited to JPG files only.

Today's update is to replace using CImg to load the files with GDI+. As a result, GIF, PNG, TIFF and BMP files are now supported. Preliminary testing suggests GDI+ is about 25% faster than CImg/libjpeg.

20151129:
Uploaded the code for the viewer. This version is WinForms/C#. The code has a bit of historic cruft to be removed...

20151201:
Upgraded the Phase 1 code/project to use OpenMP for parallelism. Each file will be processed in its own thread.

Timed on my physical machine, processing a directory tree containing 940 images at 196M in size.
Non-OpenMP: 105.66 seconds
    OpenMP:  28.36 seconds
Your mileage may vary, depending on the number of processors you have ...

20160410:
Added a CRC calculation (based on the image pixels). Allows the viewer program to indicate that a file is an actual duplicate.
