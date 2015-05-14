# AlembicImporter

Alembic ファイルのジオメトリを Unity 上で再生するプラグインです。  
Alembic は主に映画業界で使われているデータフォーマットで、フレーム毎に bake された巨大な頂点群を格納するのに用いられます。  
(詳しくは http://www.alembic.io/ )  
  
現状、実行時に Alembic ファイルからストリーミングでデータを読み込み、Mesh オブジェクトの頂点とインデックスを毎フレーム更新することで再生しています。  
このため、Alembic ファイルは Assets/StreamingAssets 以下に格納されている必要があります。  
