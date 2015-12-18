1. [Alembic?](#alembic)
2. [AlembicImporter](#alembicimporter)
3. [AlembicExporter](#alembicexporter)

# Alembic?
Alembic は主に映画業界で使われているデータフォーマットで、巨大な頂点キャッシュデータを格納するのに用いられます。  スキニングやダイナミクスなどのシミュレーション結果を必要な全フレームベイクして頂点キャッシュに変換し、それを alembic に格納してレンダラやコンポジットのソフトウェアに受け渡す、というような使い方がなされます。  
このため、Alembic のインポートやエクスポートができれば、Unity を映像制作用のレンダラやコンポジットとして使ったり、Unity で各種シミュレーションを行ってそれを他の DCC ツールに渡したりといったことができるようになります。  
(Alembic 本家: http://www.alembic.io/ )  

# AlembicImporter
![example](Screenshots/alembic_example.gif)  
パッケージ: [AlembicImporter.unitypackage](Packages/AlembicImporter.unitypackage?raw=true)

Alembic ファイルのジオメトリを Unity 上で再生するプラグインです。現在 Camera、PolyMesh、Points の再生に対応しています。  
Alembic ファイルに含まれるノード群を Unity 側で GameObject として再構築、PolyMesh を含むノードは MeshFilter や MeshRenderer も生成し、データをファイルからストリーミングして再生します。  
注意すべき点として、**Alembic ファイルは Assets/StreamingAssets 以下に置く必要があります**。これはストリーミングでデータ読み込む都合上、ビルド後も .abc ファイルがそのまま残っている必要があるためです。

上記パッケージをプロジェクトにインポート後、下記スクリーンショットのメニューより Alembic のインポートを行うことができます。  
![example](Screenshots/menu.png)  


# AlembicExporter
パッケージ: [AlembicExporter.unitypackage](Packages/AlembicExporter.unitypackage?raw=true)

Unity のシーン内のジオメトリを Alembic に書き出すプラグインです。
MeshRenderer, SkinnedMeshRenderer, ParticleSystem (point cache として出力), Camera の書き出しに対応しており、カスタムハンドラを書けば独自のデータも出力できるようになっています。

エクスポートを行うには、上記パッケージをインポート後、AlembicExporter コンポーネントを追加します。  
![example](Screenshots/AlembicExporter.png)  
Path: 出力パスを指定します。  
Archive Type: Alembic のフォーマットの指定で、通常 Ogawa のままで問題ないでしょう。  

Time Sampling Type:  
キャプチャの間隔の指定です。
これを Uniform にした場合、Alembic 側のフレーム間のインターバルは常に一定 (Time Per Sample 秒) になります。そして、これを選んでキャプチャを開始した場合した場合、**Time.maxDeltaTime が TimePerSample に固定された上、毎フレームこの間隔を待つようになります**。  
Acyclic にした場合、Unity 側のデルタタイムがそのまま Alembic 側のフレーム間のインターバルになります。この場合当然間隔はばらばらになってしまうため、映像制作には通常 Uniform を選ぶことになるでしょう。  
Start Time と Time Per Sample は、Time Sampling Type が Uniform の場合に使われる数値です。

Swap Handedness:
有効にすると 右手座標系 / 左手座標系 を入れ変える処理を挟みます。
DCC ツールの多くは Unity とは逆の座標系なので、大抵は有効にしておいたほうがいいでしょう。  

Scope: Entire Scene の場合文字通りシーン内のキャプチャ可能な全オブジェクトをキャプチャします。
Current Branch の場合その Alembic Exporter コンポーネントがついている GameObject 以下のツリーのみをキャプチャします。

Preserve Tree Structure: 有効にした場合、キャプチャ対象の親オブジェクト群の Transform も Alembic ファイルに含めます。無効の場合キャプチャ対象のみ Alembic に含めます。

Capture *** の項目は対象コンポーネントのキャプチャの有効/無効を指定します。
Ignore Disabled が有効な場合、disabled されたオブジェクトはキャプチャ対象コンポーネントであっても除外されます。

Begin Capture / End Capture, One Shot:
 キャプチャを開始 / 停止します。OneShot は現在の 1 フレームだけキャプチャします。
これらはスクリプトから BeginCapture() / EndCapture() / OneShot() を呼ぶことで同機能にアクセスでるため、独自 UI を作る場合容易に組み込めるはずです。

キャプチャの途中でオブジェクトの enabled / disabled が変わってもキャプチャは継続される点にやや注意が必要です。
また、キャプチャの途中で対象オブジェクトが削除された場合、そのオブジェクトのキャプチャは中断されますが、このようなサンプル数が不均一な Alembic ファイルは対応していないソフトウェアもあるかもしれず、避けたほうがいいシチュエーションです。



## 謝辞
- Alembic およびそれに付随するライブラリ群 (HDF5, ILMBase) を使用しています。  
  http://www.alembic.io/

## License
Copyright (C) 2015 Unity Technologies Japan, G.K.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
