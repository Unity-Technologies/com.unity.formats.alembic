# Using Alembic with Unity Animation

You can control the playback of Alembic using Unity's Animation System bound to the GameObject that contains the [Alembic Stream Player](ref_StreamPlayer.md) component.

1. Select the GameObject with the Alembic Stream Player component and open the [Animation window](https://docs.unity3d.com/Manual/animeditor-UsingAnimationEditor.html).
2. [Create an Animator component and an Animation clip](https://docs.unity3d.com/Manual/animeditor-CreatingANewAnimationClip.html) if you don't already have them.
3. [Save keyframes](https://docs.unity3d.com/Manual/animeditor-AnimatingAGameObject.html) for the **Time** property on the [**Alembic Stream Player**](ref_StreamPlayer.md) component.

   ![Saving keyframes on the property label Current Time connects the Animation clip to the Time property on the Alembic Stream Player](images/abc_anim_propertylabel.png)
4. For example, if the Alembic file lasts 16 seconds, set a key at the beginning of the clip where the **Time** property is set to 0, and another key at the end of the clip where the **Time** property is set to 16.
5. To play back the animation, use the [Animation view Play controls](https://docs.unity3d.com/Manual/animeditor-UsingAnimationEditor.html) in the Animation window or [click the Game **Play** button](https://docs.unity3d.com/Manual/Toolbar.html) from the main Unity toolbar.

Now you have a Unity Animation clip containing the animation from the Alembic file.

If you want to use the clip in the GameObject's animation state machine, add it to the [Animation Controller](https://docs.unity3d.com/Manual/Animator.html) just like any other Animation clip. On import, Unity automatically generates [an Animation clip containing Animation events](time_frameAnimEvent.md) for each frame in the Alembic file.
