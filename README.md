# AlembicImporter

![example](Screenshots/alembic_example.gif)  
Alembic ファイルのジオメトリを Unity 上で再生するプラグインです。  
Alembic は主に映画業界で使われているデータフォーマットで、フレーム毎に bake された巨大な頂点群を格納するのに用いられます。(詳しくは: http://www.alembic.io/ )  

![example](Screenshots/menu.png)  
インポートは上記スクリーンショットのメニューから行います。  
インポートすると Alembic ファイルに含まれるノード群に対応する GameObject が生成され、ポリゴンメッシュを含むノードは MeshFilter や MeshRenderer も生成されます。(subdiv や NURBS は未対応で、無視されます)
 
現状、直接 Alembic ファイルからストリーミングでデータを読み込み、Mesh オブジェクトの頂点とインデックスを毎フレーム更新することで再生しています。  
このため、**Alembic ファイルは Assets/StreamingAssets 以下に格納されている必要があります**。 
  
現状まだまだ開発中ですが、とりあえず遅いながらもジオメトリの再生ができるようになっています。  
