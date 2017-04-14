## GPUSkinning V0.2 使用说明

### 目录结构

> <img src="Document/FolderTree.png" width="150"/>

* Document：文档
* Editor：编辑器代码
* Res：演示场景中用到的资源
* Resources：编辑器要用到的所有资源
* Scenes：演示场景
* Script：所有的运行时代码

### 使用编辑器

##### 打开一个演示场景（Assets/GPUSkinning/Scenes/Adam_Sampler/Adam_Sampler.unity）

##### 选中场景中的 Adam

##### 在 Inspector 窗口中可以看到编辑器界面

> <img src="Document/Sampler.png" width="400"/>

首先必须确保 GameObject 上有 Animator 或者 Animation，这样编辑器才能对其进行采样。

Animation Name：给所采样的动作一个名字，这个名字会在保存文件时作为文件名使用。

下面有五个槽位，是不可编辑的，会指向编辑器生成的几个文件，默认是空的，因为目前为止还没有做任何操作。

Quality：蒙皮骨骼数量，数量越多，蒙皮效果越好，消耗越大。

Shader Type：默认使用的材质类型。这里提供几个基础材质，可以根据需要自行添加。

Root Bone：根骨骼节点。编辑器会从根骨骼开始，迭代所有的子骨骼，进行采样。

Sample Clips：设置要采样的 AnimationClip。

Update Or New：针对非第一次采样而言，是希望更新式的覆盖数据，还是完全创建一个新的数据。

Size：设置要采样的 AnimationClip 的数量。

Element X：是否被采样/fps/动画类型/AnimationClip

##### 开始采样

点击 Step1：PlayScene，这时场景开始播放，然后在其下方会多出一个 Start Sampler 按钮。

点击 Step2：Start Sample，开始采样数据。

如果是第一次采样数据，会弹出保存窗口，要求选择文件保存的位置。

采样完成后，原本五个空的槽位中就会有数据了（如上图所示），直接点击可定位到具体的位置。

##### 预览

点击 Preview/Edit 按钮可对采样结果进行预览。

> <img src="Document/PreviewEdit.png" width="400"/>

三色箭头是模型坐标系。

白色的框表示模型的包围盒，注意包围盒要设置正确，否则会被视锥体错误的剔除掉。

##### 展开 Bounds，对包围盒进行设置。

> <img src="Document/Bounds.png" width="400"/>

点击 Calculate Auto 按钮，会自动计算一个粗略的包围盒，然后通过下方的滑竿对包围盒进行微调。编辑完成后点击 Apply 按钮。

##### 展开 Joints，对绑点进行设置。

> <img src="Document/Joints.png" width="400"/>

对需要的绑点进行勾选。

### 使用到场景中

* 打开场景（Assets/GPUSkinning/Scenes/Adam_Sampler/Adam_Player.unity）
* 创建一个空的 GameObject。
* 在 GameObject 上挂上脚本 GPUSkinningPlayerMono。
* 将采样时生成的几个文件添加到脚本对应的位置上。
* 选中 GameObject，即可看到动画已经生效了。
* 如果设置了绑点，会在 GameObject 下自动生成绑点，将需要挂载的模型添加到绑点中即可。

> <img src="Document/JointSample.png" width="250"/>

### 使用提示以及注意

* 采样时，如果选择材质是 PBR 的，那么在生成材质时会卡顿一段时间，这是由于编译 PBR 材质导致的。
* 美术对动作调整后，这时需要对动作进行重新采样，如果有很多动作，不用对所有的动作都采样一边，只需要将需要采样的动作进行勾选，并选择更新式的数据覆盖，这样可以大大减少采样时间，提高效率。
* 对 Animator 进行采样时，直接在 Sample Clips 中设置 AnimationClip。对 Animation 进行采样时，需要在 Animation 组件中进行设置 Animation Clip。
* 每个挂载点内部都有一个 GUID 来标识，这个 GUID 是通过骨骼的 Hierarchy Path 生成的，所以如果两个骨骼的 Hierarchy Path 完全相同，就会造成绑点异常，这点需要注意的。所谓 Hierarchy Path 就是绑点骨骼到根骨骼的路径，就像文件夹路径一样。

