# AlembicImporter

![example](Screenshots/alembic_example.gif)  
Alembic ファイルのジオメトリを Unity 上で再生するプラグインです。  
Alembic は主に映画業界で使われているデータフォーマットで、フレーム毎に bake された巨大な頂点群を格納するのに用いられます。(詳しくは: http://www.alembic.io/ )  

![example](Screenshots/menu.png)  
インポートは上記スクリーンショットのメニューから行います。  
インポートすると Alembic ファイルに含まれるノード群に対応する GameObject が生成され、ポリゴンメッシュを含むノードは MeshFilter や MeshRenderer も生成されます。(subdiv や NURBS は未対応で、無視されます)

現状、Alembic ファイルから直接ストリーミングでデータを読み込み、Mesh オブジェクトの頂点とインデックスを毎フレーム更新することで再生しています。このため、**Alembic ファイルは Assets/StreamingAssets 以下に格納されている必要があります**。

現状まだまだ開発中ですが、とりあえず遅いながらもジオメトリの再生ができるようになっています。  
ちなみ冒頭の gif アニメのデータは TestData/Bifrost_milk.7z に含まれています。(そのままだと github で扱えるファイルサイズの上限 100MB を超えるため、Assets/StreamingAssets には置いていません)  


## 謝辞
- Alembic およびそれに付随するライブラリ群 (HDF5, ILMBase) を使用しています。  
  http://www.alembic.io/

## License
Copyright (C) 2015 Unity Technologies Japan, G.K.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
