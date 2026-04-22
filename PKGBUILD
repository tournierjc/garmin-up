# Maintainer: jct
pkgname=garmin-up
pkgver=0.1.0
pkgrel=1
pkgdesc="Cross-platform Garmin Express replacement built with Rust + Tauri + React"
arch=('x86_64')
url="https://github.com/tournierjc/garmin-up"
license=('MIT')
depends=(
    'webkit2gtk-4.1'
    'gtk3'
    'libayatana-appindicator'
    'librsvg'
    'udev'
    # KDE/Plasma integration
    'plasma-integration'
    'kde-cli-tools'
    'xdg-utils'
)
makedepends=(
    'rust'
    'cargo'
    'nodejs'
    'npm'
    'pkgconf'
    'binutils'
    'webkit2gtk-4.1'
    'gtk3'
    'librsvg'
    'libayatana-appindicator'
)
optdepends=(
    'kio-extras: KDE file manager integration'
    'qt6-wayland: Wayland support under KDE Plasma 6'
)
source=("$pkgname-$pkgver.tar.gz::https://github.com/tournierjc/garmin-up/archive/refs/tags/v$pkgver.tar.gz")
sha256sums=('SKIP')

prepare() {
    cd "$pkgname-$pkgver"
    npm ci --ignore-scripts
}

build() {
    cd "$pkgname-$pkgver"
    export WEBKIT_DISABLE_DMABUF_RENDERER=1
    npm run tauri build -- --no-bundle
}

package() {
    cd "$pkgname-$pkgver"

    local _binary="src-tauri/target/release/garmin-up"
    local _icon_src="src-tauri/icons"

    install -Dm755 "$_binary" "$pkgdir/usr/bin/$pkgname"

    for size in 32 128; do
        install -Dm644 "$_icon_src/${size}x${size}.png" \
            "$pkgdir/usr/share/icons/hicolor/${size}x${size}/apps/$pkgname.png"
    done
    install -Dm644 "$_icon_src/128x128@2x.png" \
        "$pkgdir/usr/share/icons/hicolor/256x256/apps/$pkgname.png"

    install -Dm644 /dev/stdin "$pkgdir/usr/share/applications/$pkgname.desktop" <<EOF
[Desktop Entry]
Name=Garmin Up
Comment=Sync and manage your Garmin devices
Exec=/usr/bin/$pkgname
Icon=$pkgname
Terminal=false
Type=Application
Categories=Utility;System;
Keywords=garmin;gps;fitness;sync;
StartupWMClass=garmin-up
EOF

    install -Dm644 /dev/stdin "$pkgdir/usr/lib/udev/rules.d/60-garmin.rules" <<EOF
# Garmin GPS devices
SUBSYSTEM=="usb", ATTRS{idVendor}=="091e", MODE="0664", GROUP="plugdev", TAG+="uaccess"
EOF
}
