# Using Animation Events with Alembic files

When an Alembic file is imported, an Animation Clip that contains Animation Events for every frame of the clip will be automatically generated.

![Animation Event Clip](images/abc_animationevents.png)

The naming convention of the Animation Clip is {modelName}_Frames.

To receive callbacks from the Alembic Animation Events clip, you simply need to have a script on your Alembic Game Object with the *AbcOnFrameChange(int)* method. The example method below would, for instance, print  the current frame of the Alembic file in the Unity console.

```
void AbcOnFrameChange (int frame) {
	Debug.Log(frame);
}
```

It is to be noted that the Animation Event clip does not contain any animation curves, only the per-frame Animation Events. As with any other Unity Animation Clip, they can be blended and layered in any [Animator Controller.](https://docs.unity3d.com/Manual/class-AnimatorController.html)

Use cases would include calling a script to change the textures on an Alembic mesh at runtime or instantiating prefabs at a specific frame during playback. Refer to the Unity [Animation Events Documentation](https://docs.unity3d.com/Manual/animeditor-AnimationEvents.html) for further reference on how to use Animation Events.


