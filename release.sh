#!/bin/sh

TARGET="
net40
net48
"

if [ -z "$1" ]
then
	version=$(git describe --abbrev=0 --tags)
else
	version="$(date +%F)-nightly"
fi

echo "Current version is $version.."
sleep 1
echo "3.."
sleep 1
echo "2.."
sleep 1
echo "1.."
sleep 1

cd "./release"
"./FoxTunes.Release.exe" "$version"
cd ..

rm -f ./release/*.zip

for target in $TARGET
do

	if [ ! -d "./release/$target" ]
	then
		echo "Source was not built: $target"
		continue
	fi

	cd "./release/$target/Main"

	echo "Setting the release type to default..";
	sed -i 's/key="ReleaseType"\s\+value=".*"/key="ReleaseType" value="Default"/' "FoxTunes.Launcher.exe.config"
	sed -i 's/key="ReleaseType"\s\+value=".*"/key="ReleaseType" value="Default"/' "FoxTunes.Launcher.x86.exe.config"

	"../../../.7z/7za.exe" a "FoxTunes-$version-$target.zip" "*.*" -r

	mv "./FoxTunes-$version-$target.zip" "../../"

	cd ..
	cd ..
	cd ..

	echo "Creating minimal package.."

	cd "./release/$target/Minimal"

	echo "Setting the release type to minimal..";
	sed -i 's/key="ReleaseType"\s\+value=".*"/key="ReleaseType" value="Minimal"/' "FoxTunes.Launcher.exe.config"
	sed -i 's/key="ReleaseType"\s\+value=".*"/key="ReleaseType" value="Minimal"/' "FoxTunes.Launcher.x86.exe.config"

	"../../../.7z/7za.exe" a "FoxTunes-$version-$target-Minimal.zip" "*.*" -r

	mv "./FoxTunes-$version-$target-Minimal.zip" "../../"

	cd ..
	cd ..
	cd ..

	echo "Creating plugins package.."

	cd "./release/$target/Plugins"

	"../../../.7z/7za.exe" a "FoxTunes-$version-Plugins-$target.zip" "*.*" -r

	mv "./FoxTunes-$version-Plugins-$target.zip" "../../"

	cd ..
	cd ..
	cd ..

done
echo

echo "All done."

