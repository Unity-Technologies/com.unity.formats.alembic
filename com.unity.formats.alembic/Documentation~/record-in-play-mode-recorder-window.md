# Record to Alembic with the Recorder window

Use the Recorder window to record a GameObject's animation to Alembic within an arbitrary time or frame interval in Play mode.

> [!NOTE]
> This scenario requires to have the [Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest) package installed in addition to the Alembic package.

To record to Alembic via the Recorder window:

1. Open the Recorder window: select **Window** > **General** > **Recorder** > **Recorder Window**.

1. In the Recorder window, add a new Alembic Clip recorder: select **Add Recorder**, and then select **Alembic Clip**.

   ![Recorder window](images/alembic-recorder-window.png)

1. In **Capture Settings**, specify the targeted export scope and the type of data you want to export.

1. In **Alembic Settings**, specify the ways to output the data as an Alembic file.

1. Set up the time or frame interval to record and the other [Alembic Clip Recorder properties](ref_Recorder.md) according to your needs.

1. Select **Start Recording**.

The recording starts and stops according to the interval you've set up in the Recorder window and the Recorder saves the animation to an Alembic file in the folder specified in **Output File** > **Path**.

## Additional resources

* [Recorder package documentation](https://docs.unity3d.com/Packages/com.unity.recorder@latest)
* [Alembic Clip Recorder properties](ref_Recorder.md)
