# Alembic Stream Player component

The Alembic Stream Player component allows you to customize import and playback.

![The Stream Player Settings window](images/abc_stream_player.png)

| *Property:*                | *Function:*                                                  |
| :------------------------- | :----------------------------------------------------------- |
| __Time Range__             | Select the range of the imported animation (in seconds) to be able to play back the animation. By default, this includes the entire animation. |
| __Time__                   | Set the time in seconds of the animation that is currently displayed on the screen. This property operates like a playhead control, as you can scrub or animate it to play the animation. Valid values are from 0 to the length of the animation. |
| __Vertex Motion Scale__    | Set the magnification factor when calculating velocity. Greater velocity means more blurring when used with Motion Blur. By default, the value is set to 1 (the velocity is not scaled). |
| __Async Load__             | Enable this option to load the file asynchronously during playback. |

> ***Note:*** Unity creates copies of .abc files under `Assets / StreamingAssets`. This is necessary for streaming data, which requires that the .abc files remain after building the project.
