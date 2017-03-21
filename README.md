# Alembic Importer / Exporter

**latest package: [AlembicImporter.unitypackage](https://github.com/unity3d-jp/AlembicImporter/releases/download/20170314/AlembicImporter.unitypackage)**  
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


Windows (32bit & 64bit)、Mac、Linux と Unity 5.2 以降で動作を確認済みです。
使用するにはまずこのパッケージをプロジェクトに import してください。  
[AlembicImporter.unitypackage](https://github.com/unity3d-jp/AlembicImporter/releases/download/20170314/AlembicImporter.unitypackage)  
(Linux の場合はこれに加えてプラグインをソースからビルドする必要があります。このリポジトリを clone し、Plugin/ に移動して CMake を用いてビルドしてください)

## Alembic Importer
![example](Screenshots/alembic_example.gif)   
Alembic ファイルに含まれるノード群を Unity 側で GameObject として再構築、PolyMesh を含むノードは MeshFilter や MeshRenderer も生成し、ファイルからデータをストリーミングして再生します。現在 Camera、PolyMesh、Points の再生に対応しています。   
注意すべき点として、Standalone でビルドする場合は **.abc ファイルは Assets/StreamingAssets 以下に置く必要があります**。これはファイルからデータをストリーミングする都合上、ビルド後も .abc ファイルがそのまま残っている必要があるためです。

パッケージをプロジェクトにインポート後、Assests メニューに Alembic インポートの項目が追加されます。  
![example](Screenshots/menu.png)  

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
  Uniform にした場合、Alembic 側のフレーム間のインターバルは常に一定 (Time Per Sample 秒) になります。映像制作の場合こちらにすべきでしょう。これを選んでキャプチャを開始した場合した場合、**Time.maxDeltaTime を書き換えてフレームレートを固定します**。Time.maxDeltaTime を独自に管理している場合注意が必要です。  
  Acyclic にした場合、Unity 側のデルタタイムがそのまま Alembic 側のフレーム間のインターバルになります。 当然間隔は一定ではなくなりますが、ゲーム進行への影響は最小限になります。主にゲームの 3D 録画を想定したモードです。  
  Start Time は Alembic 側の開始時間です。Frame Rate は Time Sampling Type が Uniform の場合の Alembic 側のフレーム間インターバルになります。  

- Xform Type  
  特に問題がなければ TRS のままにしておいてください。  
  Xform とは Alembic における Unity の Transform 相当品で、Matrix で保存するとわずかながら再生速度が速くなりますが、ソフトウェアによっては回転とスケールが同時にかかった時結果が元と変わってしまう可能性があります。  

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

- Preserve Tree Structure  
  有効にした場合、Unity 側のツリー構造をそのまま Alembic 側でも保ちます (=キャプチャ対象の親オブジェクト群の Transform も Alembic に含めます)。無効の場合、Alembic 側は全要素が Top ノード直下にぶら下がったフラットな構造になります。  
  どちらでも見た目は変わらず、無効の方が情報量は少なくなります。  

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
