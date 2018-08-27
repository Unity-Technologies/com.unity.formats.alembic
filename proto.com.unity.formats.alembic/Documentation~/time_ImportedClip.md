# Play back using an Alembic Animation clip

When the Alembic file is imported, the animation will automatically import as an Animation Clip that can be accessed expanding the imported Alembic asset.

![Expanded Alembic Asset](images/abc_expanded_asset.png)

The naming convention of the Animation Clip is {modelName}_Time.

To play back the clip on the timeline:

1. Add the Alembic object into the scene

2. Add an Animator component to the root of the Alembic GameObject hierarchy (the same object which has the [Alembic Stream Player](ref_StreamPlayer.html) component)

3. Drag the Animation Clip from the Alembic asset in the Project tab onto the Timeline

![Drag Time Clip](images/abc_drag_time_clip.png)

4. Set the Animator driving the Animation Clip to be the one added in step 2

5. Play back the animation using the [Timeline Play controls](https://docs.unity3d.com/Manual/TimelinePlaybackControls.html) in the Timeline window. 

The clip can also be played back using the Animator, by adding it to the [Animation Controller](https://docs.unity3d.com/Manual/Animator.html) just like any other clip.
