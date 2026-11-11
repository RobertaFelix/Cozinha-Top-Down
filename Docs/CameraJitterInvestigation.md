# Camera jitter investigation

Source: [Investigar tremulações (?) com resolução fora do padrão](https://app.notion.com/p/4148c09fd92780dc954fcd29fdc20a46).

## Final scope and configuration

Development currently targets Full HD (1920x1080), with Game View Scale 1x. User-facing resolution options and arbitrary window sizes are deferred by agreement with the user. No resolution menu or FPS limiter was added.

Both SampleScene and Dungeon use:

- The existing orthographic camera and Cinemachine Follow, with Rotation Composer disabled.
- The official Cinemachine Pixel Perfect extension and a 4.21875 reference lens, preserving the original rendered framing at 1080p.
- Pixel Perfect reference 240x120, 16 PPU, and Grid Snapping None. This renders at screen resolution instead of quantizing camera movement to whole art pixels.
- Cinemachine LateUpdate, with Rigidbody2D Interpolate enabled on the player.
- Zero X/Y Follow damping, avoiding delayed pixel steps after the player stops.

The existing player input, Animator parameters, animation assets and source textures remain unchanged.

## Findings

The original Rotation Composer aimed at the tracking target while the Follow position lagged behind. A render-pipeline probe measured up to 4.61 degrees of camera tilt during movement, foreshortening the sprite grid. Pixel Perfect and Cinemachine also disagreed about orthographic size: at 1080p the virtual camera requested 9.953783 while the rendered image used 4.21875.

Removing the rotation and adding the official extension eliminated that projection conflict. Removing X/Y damping eliminated late camera motion after stopping.

The blue-house source texture is Point-filtered, RGBA32, 16 PPU and has no mipmaps. An isolated comparison between its eight-vertex sprite and a Full Rect replacement rendered identical pixels. The source sprite was not modified.

An Upscale Render Texture experiment removed the odd-size warning by using an even internal buffer, but introduced another issue: camera snapping was now in whole art pixels, equivalent to eight screen pixels at 1080p. It was removed after the user's cross-monitor test exposed staircase motion. Fixed 1080p provides an even render surface without that mode. Odd free-aspect Game View sizes remain outside the current development target; the validation warning is not suppressed.

Editor preview scaling is separate from game rendering. A 1080p Game View displayed at 0.67x can make pixel widths look uneven. The Game View was maximized and verified at 1x.

## Validation

Play Mode probes used isolated saves. Render callbacks measured sequences of camera positions rather than judging static screenshots. Early probes also checked the villa and dungeon at several render sizes, zero camera tilt, lens agreement, player walking/idle animation and a single active audio listener after scene loading.

The final cadence comparison used constant movement at 4 world units/second, simulating 30, 60 and 120 FPS at 1080p, in vertical, horizontal and diagonal directions. Each case measured 35 rendered movement frames after warm-up.

| Simulated FPS | Vertical step with art-pixel upscale | Vertical step with final screen-pixel mode |
| --- | --- | --- |
| 30 | 16–24 screen pixels | 17–18 screen pixels |
| 60 | 8–16 screen pixels | 8–9 screen pixels |
| 120 | 0–8 screen pixels | 4–5 screen pixels |

All nine final direction/rate combinations passed: variation between the smallest and largest per-axis step was at most one screen pixel. This verifies reduced engine-side stepping; physical display persistence and response time were not measured by these probes.
