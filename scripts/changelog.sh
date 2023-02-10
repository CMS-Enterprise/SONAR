#!/usr/bin/env bash

FORMAT=""

usage() {
  echo "usage: ./changelog.sh [-f format] revision"
  echo ""
  echo "Generates a changelog from the specified revision to the current (HEAD) revision"
  echo ""
  echo "Supported formats:"
  echo "  html"
  echo "  markdown | md"
  echo "  default"
}

while getopts "f:" arg; do
  case $arg in
    f) FORMAT=${OPTARG} ;;
    h)
      usage
      exit 1 ;;
    ?)
      usage
      exit 1 ;;
  esac
done

# Read the revision argument
shift $(($OPTIND - 1))
REV=$1

if [ "$REV" == "" ]; then
  usage
  exit 1
fi

if [ "$FORMAT" == "html" ]; then
  git log "$1..HEAD" \
    --pretty=format:'<h2>%s</h2><div><ul><li><a href="https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/%H">view commit %h</a></li><li>Author (Committer): %an (%cn)</li><li>Date: %aD</li></ul><pre>%b</pre></div>' | sed 's#<pre></pre>##'
elif [[ "$FORMAT" == "markdown" || "$FORMAT" == "md" ]]; then
  git log "$1..HEAD" \
    --pretty=format:'### %s

* [view commit %h](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/%H)
* Author (Committer): %an (%cn)
* Date: %aD

```
%b```
' | sed '/```/{N;s/```\n```//;}'
else
  git log "$1..HEAD"
fi
