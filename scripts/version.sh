#! /usr/bin/env bash

if [[ ! -f "shared.props" ]]; then
  echo "Expected version.sh to be run from the root of the repository"
  exit 1
fi

# Get the current version
VERSION_STRING=`sed -n 's| *<VersionPrefix>\([^<]*\)</VersionPrefix>|\1|p' < shared.props`
IFS="." read -a VERSION <<< "$VERSION_STRING"

# Update shared.props
case $1 in
  major)
   VERSION[0]=$((${VERSION[0]} + 1))
   VERSION[1]=0
   VERSION[2]=0
   ;;
  minor)
   VERSION[1]=$((${VERSION[1]} + 1))
   VERSION[2]=0
   ;;
  patch)
   VERSION[2]=$((${VERSION[2]} + 1))
   ;;
  *)
    echo "usage: ./version.sh (major|minor|patch)"
    exit 1
esac

NEW_VERSION_STRING="${VERSION[0]}.${VERSION[1]}.${VERSION[2]}"
sed -i "" "s|<VersionPrefix>[^<]*</VersionPrefix>|<VersionPrefix>$NEW_VERSION_STRING</VersionPrefix>|" shared.props

# Generate Changelog

./scripts/changelog.sh -f md "$VERSION_STRING" | cat - CHANGELOG.md > ./.TMP_CHANGELOG.md
echo "## $NEW_VERSION_STRING
" | cat - ./.TMP_CHANGELOG.md > CHANGELOG.md

rm ./.TMP_CHANGELOG.md

git commit -a -m "SONAR Release v$NEW_VERSION_STRING"
