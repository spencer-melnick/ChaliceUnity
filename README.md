# Unity 3D Volumetric Cloud Renderer

![Scrolling Volumetric Cloud Render](https://github.com/spencer-melnick/spencer-melnick/raw/master/images/cloud_render.gif)

## What is it?

This is a realtime volumetric cloud ray marcher, based on the techniques used by [Guerilla Games in Horizon Zero Dawn](http://killzone.dl.playstation.net/killzone/horizonzerodawn/presentations/Siggraph15_Schneider_Real-Time_Volumetric_Cloudscapes_of_Horizon_Zero_Dawn.pdf)

Included in the project is the ray marcher and a plugin with a custom implementation of 3D Perlin/Worley noise generation.

There are two versions of the ray marcher: one using an unlit surface material, and one using a post-processing effect. The surface material has generally better quality, due to some small bugs with dark outlines on the post-processing version.

The post-processing version supports half and quarter resolution rendering for better performance at the cost of image quality. The quality loss caused by upsampling clouds is less noticable due to their naturally fuzzily-defined edges, particularly at half resolution, but the quality drop is quite noticeable at quarter resolution. The post processing version also supports early exit based on depth sampling (i.e., skipping rendering entirely if the destination pixel is going to be behind what was already rendering), and utilizes a custom depth downsample shader, using the greatest depth of all contributing pixels.

## How to use it?

!This project will not work in your game out of the box!

The current commit contains code for experimental temporal reprojection that does not work properly. If you wish to see some nice clouds, you will need to revert to the [commit for the last functional post-processing version](https://github.com/spencer-melnick/Chalice/commit/ad475f468e0bae0ae46a472d35a493a61e6efae1) or the [commit for the surface material](https://github.com/spencer-melnick/Chalice/commit/a77bd104669fb0a2b431b6dd5dcc92215a224919).
