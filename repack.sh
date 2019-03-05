#!/bin/sh
#  upm-ci-utils is a PackageManager tool used to extract tests into a separate,
#  related package.
# To install it use:
# npm install upm-ci-utils -g --registry https://api.bintray.com/npm/unity/unity-npm


pushd com.unity.formats.alembic/
upm-ci package pack
mv automation ../
cd ../automation/packages
tar -xzvf `find . -iname '*alembic-*.tgz'`
mv package alembic
tar -xzvf `find . -iname '*alembic.*.tgz'`
mv package alembic-tests
popd
