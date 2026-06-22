"""Build deterministic Hercules application assets from the approved logo source."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Images" / "Hercules-logo.png"
FONT = ROOT / "Hercules" / "Resources" / "Fonts" / "Rubik-VariableFont_wght.ttf"


def contain(image: Image.Image, size: tuple[int, int], padding: int = 0) -> Image.Image:
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    available = (size[0] - padding * 2, size[1] - padding * 2)
    copy = image.copy()
    copy.thumbnail(available, Image.Resampling.LANCZOS)
    position = ((size[0] - copy.width) // 2, (size[1] - copy.height) // 2)
    canvas.alpha_composite(copy, position)
    return canvas


def build_wordmark(logo: Image.Image, foreground: tuple[int, int, int, int], output: Path) -> None:
    canvas = Image.new("RGBA", (1400, 600), (0, 0, 0, 0))
    canvas.alpha_composite(contain(logo, (470, 470), 8), (35, 65))

    draw = ImageDraw.Draw(canvas)
    title_font = ImageFont.truetype(str(FONT), 150)
    subtitle_font = ImageFont.truetype(str(FONT), 48)
    gold = (230, 173, 64, 255)
    violet = (137, 85, 255, 255)

    draw.text((535, 160), "HERCULES", font=title_font, fill=foreground, stroke_width=1)
    draw.rounded_rectangle((542, 335, 1260, 345), radius=5, fill=gold)
    draw.text((540, 375), "ROBLOX LAUNCHER", font=subtitle_font, fill=violet)
    canvas.save(output, optimize=True)


def main() -> None:
    logo = Image.open(SOURCE).convert("RGBA")
    square = contain(logo, (512, 512), 18)

    square.save(ROOT / "Hercules" / "Hercules.png", optimize=True)
    square.save(ROOT / "Images" / "Hercules.png", optimize=True)

    icon_sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    for output in (
        ROOT / "Hercules" / "Hercules.ico",
        ROOT / "Hercules" / "Resources" / "IconHercules.ico",
    ):
        square.save(output, format="ICO", sizes=icon_sizes)

    build_wordmark(logo, (246, 244, 255, 255), ROOT / "Images" / "Hercules-full-dark.png")
    build_wordmark(logo, (25, 22, 43, 255), ROOT / "Images" / "Hercules-full-light.png")

    print("Hercules brand assets rebuilt successfully.")


if __name__ == "__main__":
    main()
