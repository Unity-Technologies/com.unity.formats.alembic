# Alembic for Unity

**Latest package: [AlembicForUnity.unitypackage](https://github.com/unity3d-jp/AlembicImporter/releases/download/20180413/AlembicForUnity.unitypackage)**  
**Do not clone this repository unless you are trying to build plugin from source.**

[English](https://translate.google.com/translate?sl=ja&tl=en&u=https://github.com/unity3d-jp/AlembicImporter) (by Google Translate)
- [Alembic?](#alembic)
- [Alembic Importer](#alembic-importer)
- [Alembic Exporter](#alembic-exporter)

## Alembic?
Alembic は主に映像業界で使われているデータフォーマットで、巨大な頂点キャッシュデータを格納するのに用いられます。  映像業界では、スキニングやダイナミクスなどのシミュレーション結果を全フレームベイクして頂点キャッシュに変換し、それを Alembic に格納してレンダラやコンポジットのソフトウェアに受け渡す、というような使い方がなされます。  
Alembic 本家: http://www.alembic.io/

近年の DCC ツールの多くは Alembic をサポートしており、Alembic のインポートやエクスポートができれば、Unity をレンダリングやコンポジットのツールとして使ったり、Unity で各種シミュレーションを行ってその結果を他の DCC ツールに渡したりといったことができるようになります。ゲームの 3D 録画のような新たな使い方も考えられます。  
本プラグインは Unity で Alembic のインポートとエクスポートを実現します。


Windows (32bit & 64bit)、Mac、Linux と Unity 2017.1 以降で動作を確認済みです。
使用するにはまずこのパッケージをプロジェクトに import します。  
[AlembicForUnity.unitypackage](https://github.com/unity3d-jp/AlembicImporter/releases/download/20180413/AlembicForUnity.unitypackage)  

## Alembic Importer
![example](Screenshots/alembic_example.gif)   
Alembic ファイルに含まれるノード群を Unity 側で GameObject として再構築、PolyMesh を含むノードは MeshFilter や MeshRenderer も生成し、ファイルからデータをストリーミングして再生します。現在 Camera、PolyMesh、Points の再生に対応しています。   

パッケージをプロジェクトにインポート後、Assets -> Import New Asset で .abc ファイルを指定すると、対応する prefab が生成されます。
Project ウィンドウでその prefab を選択することでインポート設定を変更できます。
![import settings](https://user-images.githubusercontent.com/1488611/35656675-f27445c2-073b-11e8-8365-4060cbff4896.png)
- "Normals" は .abc ファイルの法線を使うか、頂点位置を元に計算するかを設定します。デフォルトの "Compute If Missing" は .abc ファイルが法線を持っていればそれを使い、なければ計算するもので、ほとんどのケースでこれで問題ないはずです。

 - "Tangents" は接線を計算するかの設定です。.abc ファイルは接線を持たないので、計算するかしないかの 2 択になります。ただし、接線の計算には法線と UV が必要で、これらが欠けている場合は "Tangents" が "Compute" でも計算が行われないことに留意ください。  
 デフォルトで有効になっていますが、接線の計算は重い処理であり、不要な場合は無効にしておくと再生の高速化が見込めます
 
 - "Camera Aspect Ratio" は、カメラのアスペクト比に .abc ファイルのカメラパラメータを使うか、Unity 側の画面の縦横比を使うかの設定になります
 
 - "Swap Handedness", "Swap Face Winding", "Turn Quad Edges" はそれぞれ X 方向を反転するか、ポリゴンの向きを反転するか、四角形ポリゴンを三角形に分割する際に三角形の並びを反転するか、の設定になります
 
 - "Interpolate Samples" はアニメーションの補間を行うかの設定です。これが有効だと、Transform、カメラ、そしてトポロジーが変化しない (= 頂点数とインデックスが不変な) Mesh はアニメーションが補間されるようになります
 - 補間が有効な場合、もしくは .abc ファイルに velocity データが含まれる場合、シェーダに velocity データを渡すことができます。  
パッケージに付属の Alembic/Standard シェーダは、通常の Standard シェーダに上記 velocity による motion vector 生成を追加したシェーダです。ポストエフェクトの MotionBlur など、motion vector を必要とする状況で役立つでしょう。
   - 独自のシェーダに motion vector 生成機能を加えたい場合、SubShader の中に  
   UsePass "Hidden/Alembic/MotionVectors/MOTIONVECTORS"  
   の一行を足すだけで対応できるはずです。内部的な詳細を知りたい場合、AlembicMotionVectors.cginc を参照ください。(4 番目の UV に velocity データが渡されるので、それを元に 1 フレーム前の頂点位置を算出しています)

左は未加工、右は motion vector を出力して [Post Processing Stack](https://github.com/Unity-Technologies/PostProcessing) の MotionBlur をかけた状態  
![velocity](https://user-images.githubusercontent.com/1488611/35801196-c587e75a-0aae-11e8-8eda-da4eae575831.png)

- **Maya の shading group は submesh としてインポートできます**。Maya 側で "Write Face Sets" オプションを有効にしてエクスポートしておく必要があります。このオプションはデフォルトではオフであることにご注意ください。
  - Maya に限らず、Face Set のエクスポートに対応したツールであればマテリアルの設定が submesh としてインポートできるはずです。

- Maya の vertex color と additional UV set もサポートしています。
それぞれ Maya 側で "Write Color Sets", "Write UV Sets" オプションを有効にしてエクスポートしておく必要があります。これらもデフォルトではオフになっています。  
  - これらは Maya の拡張であるため、他のツールは対応していない可能性が高いです。

Maya の Alembic エクスポート設定  
![](https://user-images.githubusercontent.com/1488611/35655697-86d367e4-0736-11e8-9d28-0b3cb37fd5f0.png)


- インポートによって生成された prefab は AlembicStreamPlayer というコンポーネントを持っており、これが再生を担当します。Time パラメータを動かすと Mesh などが動くのを確認できるでしょう。これを Timeline やスクリプトから制御してアニメーションを再生します。  
Vertex Motion Scale は velocity を算出する際にかける倍率です。大きいほど velocity が大きくなり、ポストエフェクト MotionBlur で激しくブラーがかかるようになります。

- Timeline 用に Alembic Track という専用のトラックが用意されています。(Add -> UTJ.Alembic -> Alembic Track でトラックを追加、Add Alembic Shot Asset Clip でクリップを追加し、alembic オブジェクトを設定)  
![](https://user-images.githubusercontent.com/1488611/35694278-2d6026be-07c4-11e8-98b6-c0b72ff10708.gif)

- .abc ファイルは Assets/StreamingAssets 以下にコピーが作られることに留意ください。これはファイルからデータをストリーミングする都合上、ビルド後も .abc ファイルがそのまま残っている必要があるためです。

## Alembic Exporter
![example](Screenshots/AlembicExporter.gif)  

Unity のシーン内のジオメトリを Alembic に書き出します。
MeshRenderer, SkinnedMeshRenderer, ParticleSystem (point cache として出力), Camera の書き出しに対応しており、カスタムハンドラを書けば独自のデータも出力できるようになっています。  


エクスポートを行うには、AlembicExporter  コンポーネントを適当なオブジェクトに追加します。
注意すべき点として、**AlembicExporter コンポーネントは追加時に自動的に Batching を無効化します。** 有効なままだと Batching された後の Mesh 群が書き出されてしまい、場合によってはデータが数倍に膨れ上がる上に結果も変わってしまうためです。  
Batching の設定を変えたい場合、設定項目は Edit -> Project Settings -> Player の Rendering 項目の中にあります。  

以下は AlembicExporter の各項目の説明です。
<img align="right" src="Screenshots/AlembicExporter.png">
- Output Path  
  出力パスを指定します。  

- Archive Type  
  Alembic のフォーマットの指定です。大抵は Ogawa のままで問題ないと思われます。  

- Time Sampling Type  
  キャプチャの間隔の指定です。
  Uniform の場合、Alembic 側のフレーム間のインターバルは常に一定 (1 / Frame Rate 秒) になります。そして、Uniform かつ Fix Delta Time 有効でキャプチャを開始した場合した場合、**Time.maxDeltaTime を書き換えて Unity 側もデルタタイムを固定します**。映像制作の場合これは望ましい挙動のはずですが、Time.maxDeltaTime を独自に管理している場合は注意が必要です。  
  Acyclic の場合、Unity 側のデルタタイムがそのまま Alembic 側のフレーム間のインターバルになります。 当然間隔は一定ではなくなりますが、ゲーム進行への影響は最小限になります。主にゲームの 3D 録画を想定したモードです。  
  Start Time は Alembic 側の開始時間です。Frame Rate は Time Sampling Type が Uniform の場合の Alembic 側のフレーム間インターバルになります。  

- Xform Type  
  オブジェクトの位置、回転、スケールを個別に記録する (TRS) か行列で記録する (Matrix) かの選択です。TRS のままでほぼ問題ないはずです。   

- Swap Handedness  
  有効にすると 右手座標系 / 左手座標系 を入れ変える処理を挟みます。
  DCC ツールの多くは Unity とは逆の座標系なので、大抵は有効にしておいたほうがいいでしょう。  

- Swap Faces  
  面の裏表を反転します。

- Scale  
  単位変換を想定したスケール値で、例えば 0.1 にすると 1/10 サイズに変換して出力します。位置と速度に作用します。  

- Scope  
  Entire Scene の場合文字通りシーン内のキャプチャ可能な全オブジェクトをキャプチャします。
  Current Branch の場合その Alembic Exporter コンポーネントがついている GameObject 以下のツリーのみをキャプチャします。

- Ignore Disabled  
  これが有効な場合、disabled されたオブジェクトはキャプチャ対象コンポーネントであっても除外されます。

- Capture (コンポーネント名)  
  各コンポーネントのキャプチャの有効/無効を指定します。

- Begin /End Capture, One Shot  
  キャプチャを開始 / 停止します。One Shot は現在の 1 フレームだけをキャプチャします。これらはスクリプトから BeginCapture() / EndCapture() / OneShot() を呼ぶことで同機能にアクセスできます。  

現状キャプチャ対象オブジェクトはキャプチャ開始時に決定され、途中で増減はしません。なので、オブジェクトの enabled / disabled はキャプチャの途中で変わっても影響しませんし、キャプチャ開始後に生成されたオブジェクトはキャプチャされません。  
キャプチャ途中の対象オブジェクトの削除には注意が必要です。この場合そのオブジェクトのキャプチャは中断されますが、その結果できたサンプル数が不均一な Alembic ファイルはややイレギュラーな状態であり、正しく処理できないソフトウェアもあるかもしれません。避けたほうがいいシチュエーションでしょう。  

Alembic 側のノードには名前に "(0000283C)" のような ID が付与されます。これは名前の衝突を避けるための処置です。(Alembic は一つの階層に名前が同じノードが複数あってはいけないルールになっています)  
また、マテリアルは現在全くの未サポートです。
