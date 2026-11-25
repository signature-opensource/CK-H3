#!/usr/bin/env bash
#
# Builds the H3 native library (libh3.so) for the Linux runtime identifiers of CK.H3.Binaries.
# Each build runs in a Docker container, so the host needs only Docker and git.
#
# Usage: ./build-native.sh [RID...]
#   With no RID, all the Linux RIDs are built: linux-x64 linux-arm64 linux-musl-x64 linux-musl-arm64.
#   H3_VERSION selects the H3 tag (default below).
#   OUT selects the output folder (default: the runtimes folder next to this script).
#
# - linux-x64 and linux-arm64 are linked against glibc. They are built on Debian bookworm
#   (glibc 2.36): a library built against an older glibc runs on newer ones, not the reverse.
#   linux-arm64 uses the Debian cross-compiler, so it builds on any x64 host.
# - linux-musl-x64 and linux-musl-arm64 are linked against musl, for Alpine containers.
#   linux-musl-arm64 runs in an arm64 Alpine container: on an x64 host, Docker needs QEMU
#   emulation for it (Docker Desktop has it; on Linux, see https://docs.docker.com/build/building/multi-platform/).
#
# The win-x64 h3.dll is not built by this script.
#
set -euo pipefail

H3_VERSION="${H3_VERSION:-v4.4.1}"
H3_REPO="https://github.com/uber/h3.git"
DEBIAN_IMAGE="debian:bookworm"
ALPINE_IMAGE="alpine:3.23"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT="${OUT:-$HERE/runtimes}"

# Only the library: no tests, tools, benchmarks, docs, format or lint steps.
CMAKE_ARGS="-DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DBUILD_TESTING=OFF \
-DBUILD_BENCHMARKS=OFF -DBUILD_FUZZERS=OFF -DBUILD_FILTERS=OFF -DBUILD_GENERATORS=OFF \
-DENABLE_DOCS=OFF -DENABLE_FORMAT=OFF -DENABLE_LINTING=OFF"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

echo "Cloning H3 $H3_VERSION..."
git -c advice.detachedHead=false clone --quiet --depth 1 --branch "$H3_VERSION" "$H3_REPO" "$WORK/src"

# build <rid> <docker platform> <image> <install command> <extra cmake args>
build() {
    local rid="$1" platform="$2" image="$3" install="$4" extra="$5"
    echo "Building $rid with $image ($platform)..."
    mkdir -p "$OUT/$rid/native"
    docker run --rm --platform "$platform" \
        -v "$WORK/src:/src:ro" \
        -v "$OUT/$rid/native:/out" \
        "$image" sh -euc "
            $install >/dev/null
            cmake -S /src -B /tmp/build $CMAKE_ARGS $extra
            cmake --build /tmp/build --target h3 --parallel
            cp -L /tmp/build/lib/libh3.so /out/libh3.so
            chown $(id -u):$(id -g) /out/libh3.so
        "
    echo "  -> $OUT/$rid/native/libh3.so"
}

DEBIAN_INSTALL="apt-get update -qq && apt-get install -y -qq --no-install-recommends cmake make"
ALPINE_INSTALL="apk add --no-cache -q gcc musl-dev cmake make"

RIDS=("$@")
[ ${#RIDS[@]} -eq 0 ] && RIDS=(linux-x64 linux-arm64 linux-musl-x64 linux-musl-arm64)

for rid in "${RIDS[@]}"; do
    case "$rid" in
        linux-x64)
            build "$rid" linux/amd64 "$DEBIAN_IMAGE" "$DEBIAN_INSTALL gcc libc6-dev" "" ;;
        linux-arm64)
            build "$rid" linux/amd64 "$DEBIAN_IMAGE" "$DEBIAN_INSTALL gcc-aarch64-linux-gnu libc6-dev-arm64-cross" \
                "-DCMAKE_SYSTEM_NAME=Linux -DCMAKE_SYSTEM_PROCESSOR=aarch64 -DCMAKE_C_COMPILER=aarch64-linux-gnu-gcc" ;;
        linux-musl-x64)
            build "$rid" linux/amd64 "$ALPINE_IMAGE" "$ALPINE_INSTALL" "" ;;
        linux-musl-arm64)
            build "$rid" linux/arm64 "$ALPINE_IMAGE" "$ALPINE_INSTALL" "" ;;
        *)
            echo "Unknown RID '$rid'." >&2; exit 1 ;;
    esac
done
