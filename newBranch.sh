#!/bin/bash

git checkout -B "$1" || exit 1
git add . || exit 1
git commit --allow-empty -m "Init Branch $1" || exit 1
git push --set-upstream origin "$1" || exit 1



