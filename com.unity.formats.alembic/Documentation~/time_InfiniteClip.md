# Record and play back the Alembic using an Infinite clip

To control the playback of Alembic, use an **Infinite clip** on a Timeline **Animation Track** bound to the GameObject that contains the [Alembic Stream Player](ref_StreamPlayer.md) component.

1. Select the GameObject with the Alembic Stream Player component and [open the Timeline window](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/tl-window.html).
2. [Create a Director component and a Timeline Asset](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/wf-create-instance.html) if you don't already have them.
3. Create an Animation Track and assign the GameObject with the Alembic Stream Player.
4. Begin recording, and save keys for the **Time** property on the **Alembic Stream Player** component (see how to [Record basic animation](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/wf-record-anim.html)). For example, if the Alembic file lasts 16 seconds, set a key at the beginning of the clip where the **Time** property is set to 0, and another key at the end of the clip where the **Time** property is set to 16.
5. Play back the animation using the [Timeline Play controls](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/tl-play-ctrls.html) in the Timeline window.

![Controlling Stream Player With Infinite Clip](images/abc_infinite_clip.png)

If you want to use the animation in the GameObject's animation state machine, you can [convert the Infinite clip into an animation clip](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/wf-convert-infinite.html) and add it to the [Animation Controller](https://docs.unity3d.com/Manual/Animator.html).
