# Using the Alembic Clip Recorder

Another way to export alembics out of Unity is to use the Alembic Clip Recorder, which is an extension of the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html). It becomes available as soon as you install both the _Alembic for Unity_ and _Unity Recorder_ packages.

The Alembic Clip Recorder includes the overall functionality of the Unity Recorder in addition to its own [Recorder properties](ref_Recorder.md):

* You can use the Alembic Clip Recorder as any other recorder, either from the Unity Recorder window or through a Recorder Clip in a [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html) track.
* You can use more than one recorder of any type at the same time to simultaneously capture different data.
* The ways to start and stop the Alembic Clip Recorder are the same as for any other recorder type.

To set up an Alembic Clip Recorder, you need to perform the following main steps:

1. When adding a new recorder (either through the Recorder window or a Recorder Clip within Timeline), select **Alembic Clip**.

2. Set the [Recorder properties](ref_Recorder.md) specific to the Alembic Clip Recorder.

3. Set the global Recording Properties (shared with the other recorders, such as the frames to record).

> **Note:** The [Unity Recorder documentation](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) provides the generic instructions to add a recorder and set up the global Recording Properties.
