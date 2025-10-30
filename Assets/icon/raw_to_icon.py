import argparse
import io
import os
import struct
import sys
from typing import Dict, List, Tuple

try:
    from PIL import Image
except ImportError:
    print("Pillow is required. Install with: pip install pillow")
    sys.exit(1)


def find_images(src_dir: str) -> List[str]:
    if not os.path.isdir(src_dir):
        print(f"Source folder not found: {src_dir}")
        sys.exit(1)
    files: List[str] = []
    for name in os.listdir(src_dir):
        lower = name.lower()
        if lower.endswith(".png") or lower.endswith(".bmp"):
            files.append(os.path.join(src_dir, name))
    if not files:
        print(f"No PNG/BMP files found in: {src_dir}")
        sys.exit(1)
    return files


def center_crop_to_square(image: Image.Image) -> Image.Image:
    width, height = image.size
    if width == height:
        return image
    side = min(width, height)
    left = (width - side) // 2
    top = (height - side) // 2
    return image.crop((left, top, left + side, top + side))


def load_unique_sized_images(files: List[str]) -> List[Tuple[int, Image.Image]]:
    # Keep one image per size; if duplicates, prefer PNG over BMP
    by_size: Dict[int, Tuple[bool, Image.Image]] = {}

    for path in files:
        is_png = path.lower().endswith(".png")
        with Image.open(path) as im:
            im = im.convert("RGBA")
            im = center_crop_to_square(im)

            w, h = im.size
            # Clamp to ICO max 256x256
            max_side = min(256, max(w, h))
            if w != h or max_side != w:
                im = im.resize((max_side, max_side), Image.LANCZOS)

            size_px = im.size[0]
            existing = by_size.get(size_px)
            if existing is None or (is_png and not existing[0]):
                by_size[size_px] = (is_png, im.copy())

    if not by_size:
        print("No usable images after processing.")
        sys.exit(1)

    sizes_sorted_desc = sorted(by_size.keys(), reverse=True)
    return [(s, by_size[s][1]) for s in sizes_sorted_desc]


def encode_images_as_png_bytes(sized_images: List[Tuple[int, Image.Image]]) -> List[Tuple[int, bytes]]:
    results: List[Tuple[int, bytes]] = []
    for size_px, img in sized_images:
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        results.append((size_px, buf.getvalue()))
    return results


def write_ico_from_png_blobs(output_path: str, png_blobs: List[Tuple[int, bytes]]) -> None:
    # ICO header: ICONDIR (6 bytes) + n * ICONDIRENTRY (16 bytes each) + image data blocks
    count = len(png_blobs)
    header_size = 6 + 16 * count

    # Prepare directory entries with offsets
    entries: List[Tuple[int, int, int, int, int, int, int, int]] = []
    # Fields: width_byte, height_byte, color_count, reserved, planes, bit_count, size_in_bytes, offset

    offset = header_size
    for size_px, data in png_blobs:
        width_byte = 0 if size_px >= 256 else size_px
        height_byte = 0 if size_px >= 256 else size_px
        color_count = 0
        reserved = 0
        planes = 1
        bit_count = 32
        size_in_bytes = len(data)
        entries.append((width_byte, height_byte, color_count, reserved, planes, bit_count, size_in_bytes, offset))
        offset += size_in_bytes

    with open(output_path, "wb") as f:
        # ICONDIR
        f.write(struct.pack("<HHH", 0, 1, count))
        # ICONDIRENTRY list
        for (w, h, cc, rsv, planes, bpp, sz, off) in entries:
            f.write(struct.pack("<BBBBHHII", w, h, cc, rsv, planes, bpp, sz, off))
        # Image data blocks (PNG payloads)
        for _, data in png_blobs:
            f.write(data)


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a multi-size .ico from PNG/BMP sources in a folder.")
    parser.add_argument("--src", default="icons", help="Folder next to this script containing source images (default: icons)")
    parser.add_argument("--out", default="icon.ico", help="Output .ico filename (default: icon.ico)")
    args = parser.parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    src_dir = os.path.join(script_dir, args.src)
    out_path = os.path.join(script_dir, args.out)

    files = find_images(src_dir)
    sized_images = load_unique_sized_images(files)
    png_blobs = encode_images_as_png_bytes(sized_images)
    write_ico_from_png_blobs(out_path, png_blobs)

    sizes_list = ", ".join([f"{s}x{s}" for s, _ in png_blobs])
    print(f"Wrote {out_path} with sizes: {sizes_list}")


if __name__ == "__main__":
    main()