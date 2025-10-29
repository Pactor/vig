# 🪨 Voxel Image Generator

**Voxel Image Generator** is a C# console application that automates the creation of texture maps used for **Space Engineers** custom ores and voxels.

---

## 🎯 Overview
Given a single **seamless base PNG** image, the tool automatically generates all the required texture variants used by Space Engineers voxel materials:

- `_cm` – Color map (base texture)  
- `_ng` – Normal and gloss map  
- `_add` – Additional detail map  
- `_distance` – Distance / depth map  
- `_xz`, `_y` – Side and top directional textures  
- `_thumbnail` – Small preview image (164×12)

These outputs follow the naming conventions expected by the game for new ore or terrain materials.

---

## ⚙️ Usage
VoxelImageGenerator.exe <input.png> [options]

VoxelImageGenerator.exe iron.png --metal 40 --gloss 1.2 --blur 5 --contrast 0.6 --thumb 164 12

🔧 Command-Line Options
Switch	Default	Description
--metal <int>	30	Adjusts metalness offset. Higher values increase the metallic effect in _cm and _ng maps.
--gloss <float>	1.0	Multiplier for gloss intensity. Use >1.0 for shinier surfaces.
--blur <int>	3	Controls blur radius for the _distance map. Larger values give a smoother depth effect.
--contrast <float>	0.5	Adjusts image contrast across generated maps. 0.0 = flat, 1.0 = full contrast.
--thumb	— int width int height 164 12	Generates the thumbnail (_thumbnail.png) at 164×12 pixels. Default size can be changed with arguments.

🧱 Requirements

Windows with .NET Framework 4.8

Input image must be a seamless 1:1 PNG

💡 Notes

_cm texture remains true to the source image, but attempts to add a metal mask alpha

_distance map uses Gaussian blur to simulate depth.

Perfect for modders adding custom ores or terrain materials to Space Engineers.

📜 License

MIT License – free to use and modify.