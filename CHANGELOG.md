# Changes in Alembic for Unity

## [1.0.0-preview.7] - 2018-11-28
### Changes
- Fixed Windows plugin platform settings
- Apply Transform On Points is now ON by default to be consistent with mesh behaviour
- Point clouds are no longer imported by default
- Bug fix: changes to time range were not persisted
- Updated label of assumeNonSkinnedMeshRendersAreConstant to "Static MeshRenderers" to prevent cropping at default width

### Known Issues
- Camera objects aren't using Physical Camera mode
- Points renderer motion vector shader doesn't work
- Camera nodes are still created as GameObjects even if we don't import cameras
- AlembicWaitForEndOfFrame script is visible from the Component list in the inspector

## [1.0.0-preview.6] - 2018-11-22
- Apply transform to points renderer by default
- Don't import point clouds by default

## [1.0.0-preview.4] - 2018-10-05
- Added missing Linux dso

## [1.0.0-preview.3] - 2018-09-26
- Reorganized to conform to Unity's package standards
- Updated Alembic to 1.7.9 (Ogawa is now memory-mapped by default)

## [1.0.0-preview.2] - 2018-09-20
- Fixed AlembicStreamPlayerEditor warning in Unity 2018.3

## [1.0.0-preview.1] - 2018-09-07
- Added Documentation for imported animation clips
- Removed UTJ/Alembic/Exporter component menu
- Normalized all line endings

## [1.0.0-preview] - 2018-08-21
- Updated Documentation
- Contains all changes up until [release 20180413](https://github.com/unity3d-jp/AlembicForUnity/releases/tag/20180413) on github

## [0.2.0] - 2018-06-22
- Internalized the API
- Updated Documentation
- Changed namespaces to Unity[Editor|Engine].Formats.Alembic.[Exporter|Importer]
- Removed Alembic Camera Params component

## [0.1.2] - 2018-03-19

- Initial version for Package Manager
- Corresponds to [release 20180222](https://github.com/unity3d-jp/AlembicImporter/releases/tag/20180222) on github
