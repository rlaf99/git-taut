#!/bin/sh

usage() {
  echo "$0 sectors mount_target"
}

if [ "$#" -lt 2 ]; then
  echo "wrong number of arguments" 2>&1
  usage
  exit 1
fi

# 20M = 40960 sectors
# 30m = 61440 sectors
SECTORS=$1       # a sector is 512 bytes
TARGET=$2
# echo "${SECTORS} ${TARGET}"
mkdir -p $TARGET
RAMDEV=`hdiutil attach -nomount ram://$SECTORS`
newfs_hfs $RAMDEV
mount -t hfs $RAMDEV $TARGET
echo "How to cleanup"
echo "umount ${TARGET}\nhdiutil detach ${RAMDEV}" |  tee "$TARGET/detach"