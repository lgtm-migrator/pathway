#!/bin/bash
# server=build.palaso.org
# build_type=bt362
# root_dir=..
# Auto-generated by https://github.com/chrisvire/BuildUpdate.
# Do not edit this file by hand!

cd "$(dirname "$0")"

# *** Functions ***
force=0
clean=0

while getopts fc opt; do
case $opt in
f) force=1 ;;
c) clean=1 ;;
esac
done

shift $((OPTIND - 1))

copy_auto() {
if [ "$clean" == "1" ]
then
echo cleaning $2
rm -f ""$2""
else
where_curl=$(type -P curl)
where_wget=$(type -P wget)
if [ "$where_curl" != "" ]
then
copy_curl $1 $2
elif [ "$where_wget" != "" ]
then
copy_wget $1 $2
else
echo "Missing curl or wget"
exit 1
fi
fi
}

copy_curl() {
echo "curl: $2 <= $1"
if [ -e "$2" ] && [ "$force" != "1" ]
then
curl -# -L -z $2 -o $2 $1
else
curl -# -L -o $2 $1
fi
}

copy_wget() {
echo "wget: $2 <= $1"
f1=$(basename $1)
f2=$(basename $2)
cd $(dirname $2)
wget -q -L -N $1
# wget has no true equivalent of curl's -o option.
# Different versions of wget handle (or not) % escaping differently.
# A URL query is the only reason why $f1 and $f2 should differ.
if [ "$f1" != "$f2" ]; then mv $f2\?* $f2; fi
cd -
}


# *** Results ***
# build: pathway-precise-64-continuous (bt362)
# project: Pathway
# URL: http://build.palaso.org/viewType.html?buildTypeId=bt362
# VCS: https://github.com/sillsdev/pathway.git [develop]
# dependencies:
# [0] build: palaso-linux64-libpalaso-3.1 Continuous (bt322)
#     project: libpalaso
#     URL: http://build.palaso.org/viewType.html?buildTypeId=bt322
#     clean: false
#     revision: pathway.tcbuildtag
#     paths: {"L10NSharp.dll"=>"lib", "SIL.Core.dll"=>"lib", "SIL.Core.dll.config"=>"lib", "SIL.Core.dll.mdb"=>"lib", "SIL.WritingSystems.dll"=>"lib", "SIL.WritingSystems.dll.mdb"=>"lib", "icu.net.dll"=>"lib", "icudt54.dll"=>"lib", "icuin54.dll"=>"lib", "icuuc54.dll"=>"lib"}
#     VCS:  https://github.com/sillsdev/libpalaso.git [libpalaso-3.1]

# make sure output directories exist
mkdir -p ../lib

# download artifact dependencies
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/L10NSharp.dll ../lib/L10NSharp.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/SIL.Core.dll ../lib/SIL.Core.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/SIL.Core.dll.config ../lib/SIL.Core.dll.config
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/SIL.Core.dll.mdb ../lib/SIL.Core.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/SIL.WritingSystems.dll ../lib/SIL.WritingSystems.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/SIL.WritingSystems.dll.mdb ../lib/SIL.WritingSystems.dll.mdb
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/Spart.dll ../lib/Spart.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/icu.net.dll ../lib/icu.net.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/icudt54.dll ../lib/icudt54.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/icuin54.dll ../lib/icuin54.dll
copy_auto http://build.palaso.org/guestAuth/repository/download/bt322/pathway.tcbuildtag/icuuc54.dll ../lib/icuuc54.dll
# End of script
