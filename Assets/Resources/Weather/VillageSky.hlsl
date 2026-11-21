#ifndef UNDERGRUB_VILLAGE_SKY_INCLUDED
#define UNDERGRUB_VILLAGE_SKY_INCLUDED

// Samples are indexed by map cell, so the camera never carries a drop or a cloud with it.
float SkyHash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float SkyNoise(float2 p)
{
    float2 cell = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(SkyHash(cell), SkyHash(cell + float2(1,0)), f.x),
                lerp(SkyHash(cell + float2(0,1)), SkyHash(cell + 1), f.x), f.y);
}

// Separate, softly lobed silhouettes with clear ground between them. Each row
// starts at a different offset so cloud banks do not form a marching grid.
float CloudShadow(float2 world)
{
    float2 drift = world - float2(.82, .16)*_Time.y;
    float2 spacing = float2(12, 7.5);
    float row = floor(drift.y/spacing.y);
    drift.x += SkyHash(float2(row, 19))*spacing.x;
    float2 cell = floor(drift/spacing);
    float2 p = (frac(drift/spacing)-.5)*spacing;
    float seed = SkyHash(cell+float2(21,12));
    p -= float2((seed-.5)*1.2, (SkyHash(cell+27)-.5)*1.5);
    p.y += p.x*(seed-.5)*.17;
    p /= lerp(.77,1.08,seed);
    float base = length((p-float2(0,-.15))/float2(2.70,.90))-1;
    float left = length((p-float2(-1.75,.14))/float2(1.28,.97))-1;
    float top = length((p-float2(-.49,.51))/float2(1.65,1.12))-1;
    float right = length((p-float2(1.30,.26))/float2(1.49,.92))-1;
    float shape = min(min(base,left),min(top,right));
    return 1-smoothstep(-.15,.14,shape);
}

void RainEvent(float2 world, out float2 delta, out float phase)
{
    float2 cell = floor(world / 2);
    float t = _Time.y * .9 + SkyHash(cell + 32);
    float2 key = cell + floor(t) * float2(51, 17);
    float2 landing = cell * 2 + float2(.3 + 1.38 * SkyHash(key), .24 + .67 * SkyHash(key + 9));
    delta = world - landing;
    phase = frac(t);
}

float RainStreak(float2 delta, float phase)
{
    float remaining = saturate(1 - phase / .36);
    float2 tip = remaining * float2(.36, .9);
    float2 p = delta - tip;
    return (1 - step(.046, abs(p.x - p.y * .40))) * step(0,p.y) * (1 - step(.24,p.y)) * (1 - step(.36, phase));
}

float RainImpact(float2 delta, float phase, bool water)
{
    float age = (phase - .36) / (water ? .58 : .23);
    if (age < 0 || age > 1) return 0;
    float radius = length(delta * float2(1,1.9));
    if (water)
        return (1 - smoothstep(.032,.065,abs(radius - lerp(.045,.39,age)))) * (1-age) * .72;
    // Three pixels kick outward from the exact point where the streak reached the soil.
    float2 wings = float2(abs(delta.x), delta.y) - float2(.03 + .16*age, .01 + sin(age*3.14)*.12);
    return (1 - smoothstep(.025,.057,length(wings))) * (1-age) * .9;
}

#endif
