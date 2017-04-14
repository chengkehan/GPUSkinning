## GPUSkinning to improve performance.

### v0.2.1

TODO: More Features

* Memory Optimization

* Individual Difference Animation

* GPU Instancing

* Skinning Instancing

* Root Bone Animation

* Animation Blend

### [v0.2](https://github.com/chengkehan/GPUSkinning/releases)

* Complete (See tag v0.2)

* [Document](Assets/GPUSkinning/Document/Document.pdf)

### v0.2 ( [dev](https://github.com/chengkehan/GPUSkinning/tree/v0.2-dev) )

TODO: 

* No hard-code. 

* Easier to use(editor tool). 

* Compatible with various animation types(legacy, generic, humanoid, animation).

* New GPUSkinning api.

### [v0.1-alpha](https://github.com/chengkehan/GPUSkinning/tree/v0.1-alpha)

_Experimental Version.This version is not for production use, just to verify feasibility of idea._

Record data of bone animation from Animation Component, customize GPUSkinning rather than Unity built-in.

4 Ways to implement: 

1. Compute bone hierarchy matrix of current frame in Application code -> push **Matrix Array** to GPU -> Skinning in Vertex Shader -> Next Frame(Goto first step)

2. Compute bone hierarchy matrix of all frames -> Store all matrices into a **Texture2D** -> Skinning in Vertex Shader(Extract all matrices of current frame to skinning)(Using **"additionalVertexStreams"** to get individual animtion)

3. Base on Way1 and Way2, Using **GPU Instancing** to get the minimum Batches, Using **CullingGroup** to implement **Level of Detail** to reduce triangle-rendering overhead.

4. Compute bone hierarchy matrix of all frames -> Store data into a **ComputeBuffer** -> Skinning in ComputeShader(DirectX 11)

> ![img](screenshot.gif)
>
> screenshot

> ![img](screenshot2.gif)
>
> Adding a new feature that we can attach weapon to hand joint in this example.

[http://chengkehan.github.io/GPUSkinning.html](http://chengkehan.github.io/GPUSkinning.html)

[http://chengkehan.github.io/GPUSkinning2.html](http://chengkehan.github.io/GPUSkinning2.html)

[http://chengkehan.github.io/GPUSkinning3.html](http://chengkehan.github.io/GPUSkinning3.html)

[http://chengkehan.github.io/GPUSkinning4.html](http://chengkehan.github.io/GPUSkinning4.html)
