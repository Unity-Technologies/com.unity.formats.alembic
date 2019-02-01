#!/bin/sh
pushd build/install/com.unity.formats.alembic/
#npm install upm-ci-utils -g --registry https://api.bintray.com/npm/unity/unity-npm
upm-ci package pack
mv automation ../
cd ../automation/packages
tar -xzvf `find . -iname '*alembic-*.tgz'`
mv package alembic
tar -xzvf `find . -iname '*alembic.*.tgz'`
mv package alembic-tests
popd
