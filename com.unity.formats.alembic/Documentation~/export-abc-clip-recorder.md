# Export via an Alembic Clip Recorder

Export a Unity GameObject as an Alembic file using an Alembic Clip Recorder.

>[!NOTE]
>The Alembic Clip Recorder is an extension of the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html). It becomes available as soon as you install both the _Alembic for Unity_ and _Unity Recorder_ packages.

## Alembic Clip Recorder functionality

The Alembic Clip Recorder includes the overall functionality of the Unity Recorder in addition to its own [Recorder properties](ref_Recorder.md):

* You can use the Alembic Clip Recorder as any other recorder, either via the Recorder window or via a Recorder Clip in [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html).
* You can use more than one recorder of any type at the same time to simultaneously capture different data.
* The ways to start and stop the recording of Alembic files are the same as for any other recorder type.

## Setup guidelines

To set up an Alembic Clip Recorder:

1. Choose a recording method: either a recording session in the Recorder window or a Recorder Clip in Timeline.

2. Set the time or frame interval to record in according to the chosen recording method:

   * If you're using the Recorder window, set up the Recording Mode values, OR

   * If you're using a Recorder Clip in Timeline, adjust the clip boundaries.

2. Add or select the recorder type according to the chosen method: **Alembic Clip**.

   ![](images/alembic-recorder-window.png)<br />
   _Example: Alembic Clip Recorder Settings in the context of the Recorder window._

3. Set the [Recorder properties](ref_Recorder.md) according to your output needs:

   * In **Capture Settings**, specify the targeted export scope and the type of data you want to export.

   * In **Alembic Settings**, specify the ways to output the data as an Alembic file.

   * In **Output File**, specify the name and location of the file to save the data to.

4. Start the recording according to the chosen recording method:

   * If you're using the Recorder window, select **Start Recording**.

   * If you're using a Recorder Clip in Timeline, enter Play mode.

## Additional information

The [Unity Recorder documentation](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) provides the generic instructions to set up your project for recording according to your workflow needs.
