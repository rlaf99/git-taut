#!/bin/sh

usage() {
  echo "Create a ramdisk and mount it to a directory on macOS"
  echo ""
  echo "Usage: "
  echo "$0 sectors mount_target"
  echo ""
  echo "One sector is 512 bytes, examples:"
  echo "  20M = 40960 sectors"
  echo "  30M = 61440 sectors"
  echo "  50M = 102400 sectors"
}

if [ "$#" -lt 2 ]; then
  echo "Error: wrong number of arguments" 2>&1
  echo "" 2>&1
  usage
  exit 1
fi

SECTORS=$1       # a sector is 512 bytes
TARGET=$2
# echo "${SECTORS} ${TARGET}"
mkdir -p $TARGET
RAMDEV=`hdiutil attach -nomount ram://$SECTORS`
newfs_hfs $RAMDEV
mount -t hfs $RAMDEV $TARGET
echo "How to cleanup"
echo "umount ${TARGET}\nhdiutil detach ${RAMDEV}" |  tee "$TARGET/detach"