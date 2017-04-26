## GPUSkinning V0.2.1 使用说明

### 功能简介

本工具将骨骼动画 Bake 到 Texture2D 中，使用 Texture2D 中存储的骨骼动画数据在 GPU 中进行蒙皮，降低 Unity 内置的骨骼动画的开销，让场景中可以承载更多的骨骼动画单位。

在 Unity 中，只要是以 Animation 或 Animator 驱动，带动 SubTransform 进行的运动的动画，都可以被该工具记录下来。所以 Legacy，Generic，Humanoid 这些动画类型都是被支持的。

个体差异动画已经可以使用了。当开启时，多个相同的模型动作不会出现完全同步现象，默认是开启的。也可以手动关闭该功能，来表现方阵中每个单位的整齐的动作。

GPU Instancing 已经被内置。当硬件支持时，会自动开启 GPU Instancing，以达到减少 Draw Call 的目的。当硬件不支持时，会回退到普通模式。

Unity 的 Root Motion 可以让位移动画变得更自然，防止类似滑步等问题的出现。同样 Root Motion 也被本工具所支持。

在 Unity 中通过设置 Optimize Game Objects，来避免对不必要的 Transform 进行刷新。同样，在本工具中，所有的骨骼节点默认都是不会生成的，也就是说在 Hierarchy 中无法看到，需要在编辑面板中勾选需要显示哪些骨骼，这样就可以将武器之类的模型挂载到对应的骨骼节点上。

### 目录结构

> <img src="Document/FolderTree.png" width="100"/>

* Document：文档
* Editor：编辑器代码
* Res：演示场景中用到的资源
* Resources：编辑器要用到的所有资源
* Scenes：演示场景
* Script：所有的运行时代码

### 使用编辑器

打开一个演示场景（Assets/GPUSkinning/Scenes/Adam_Sampler/Adam_Sampler.unity）

选中场景中的 Adam

在 Inspector 窗口中可以看到编辑器界面

> <img src="Document/Sampler.png" width="400"/>

首先必须确保 Adam GameObject 上有 Animator 或者 Animation，这样编辑器才能对其进行采样。

Animation Name：给所采样的动作一个名字，这个名字会在保存文件时作为文件名使用。

下面有五个槽位，是不可编辑的，会指向编辑器生成的几个文件，默认是空的，当骨骼动画采样完成后，这几个槽位中将会显示对应的内容。

Quality：蒙皮骨骼数量，数量越多，蒙皮效果越好，消耗越大。

Shader Type：默认使用的材质类型。这里提供几个基础材质，可以根据需要自行修改添加。

Root Bone：根骨骼节点。编辑器会从根骨骼开始，迭代所有的子骨骼，进行采样。

Sample Clips：设置要采样的 AnimationClip。

Size：设置要采样的 AnimationClip 的数量。

FPS：0 表示使用 AnimationClip 中默认的帧率进行采样，可以填入想要的采样率。

Wrap Mode：动画播放类型。

Anim Clip：被采样的 Animation Clip。

Root Motion：采样时是否开启 Root Motion。

**注意：在开始采样前，请确保 Animation Clip 本身的 Loop Time 是被勾选的，否则可能会采样失败。**

##### 开始采样

点击 Step1：PlayScene，这时场景开始播放，然后在其下方会多出一个 Start Sampler 按钮。

点击 Step2：Start Sample，开始采样数据。

如果是第一次采样数据，会弹出保存窗口，要求选择文件保存的位置。选择一个文件夹，采样所生成的数据都会保存在指定的文件夹内。

采样完成后，原本五个空的槽位中就会有数据了（如上图所示），直接点击可定位到具体的位置。

##### 预览

点击 Preview/Edit 按钮可对采样结果进行预览。

> <img src="Document/PreviewEdit.png" width="400"/>

下拉菜单，选择预览的动作名字。

三色箭头是模型坐标系。

白色的框表示模型的包围盒。注意，Unity 在视锥体裁切时会将包围盒在视锥体外的不送入渲染管线，所以包围盒要设置正确，否则会被视锥体错误的剔除掉。

##### 展开 Bounds，对包围盒进行设置。

> <img src="Document/Bounds.png" width="400"/>

点击 Calculate Auto 按钮，会自动计算一个粗略的包围盒，然后通过下方的滑竿对包围盒进行微调。编辑完成后点击 Apply 按钮。

##### 展开 Root Motion

只有当前预览的动作，在采样时勾选了 Root Motion，才会显示这部分UI。

点击 Apply Root Motion，预览效果。

##### 展开 Joints，对绑点进行设置。

> <img src="Document/Joints.png" width="400"/>

对需要的绑点进行勾选。被勾选的绑点将会显示 Hierarchy 中，可以直接将武器挂载到绑点上，详见下文。

### 将采样数据应用到场景中

* 打开场景（Assets/GPUSkinning/Scenes/Adam_Sampler/Adam_Player.unity）
* 创建一个空的 GameObject。
* 在 GameObject 上挂上脚本 GPUSkinningPlayerMono。
* 将采样时生成的几个文件添加到脚本对应的位置上。
* 根据需要勾选 Apply Root Motion。
* 选中 GameObject，即可看到动画已经生效了。注意，Root Motion 效果需要运行播放器才能看到。
* 如果设置了绑点，会在 GameObject 下自动生成绑点，将需要挂载的模型添加到绑点中即可。

> <img src="Document/JointSample.png" width="250"/>

### 使用提示以及注意

* 采样时，如果选择材质是 PBR 的，那么在生成材质时会卡顿一段时间，这是由于编译 PBR 材质导致的。
* 对 Animator 进行采样时，直接在 Sample Clips 中设置 AnimationClip。对 Animation 进行采样时，需要在 Animation 组件中进行设置 Animation Clip。
* 每个挂载点内部都有一个 GUID 来标识，这个 GUID 是通过骨骼的 Hierarchy Path 生成的，所以如果两个骨骼的 Hierarchy Path 完全相同，就会造成绑点异常，这点需要注意，可以通过对绑点骨骼取一些有意义的名字来避免。所谓 Hierarchy Path 就是绑点骨骼到根骨骼的路径，就像文件夹路径一样。

