#ifndef HEIGHTCODE_INCLUDED
#define HEIGHTCODE_INCLUDED

//把高度分解成(RGBA)4个数值
fixed4 EncodeHeightmap(fixed fHeight)
{
	fixed h = fHeight;
	fixed positive = fHeight > 0 ? fHeight : 0;
	fixed negative = fHeight < 0 ? -fHeight : 0;

	fixed4 color = 0;

	color.r = positive;
	color.g = negative;

	//把小数点中比较低的位数放在(b,a)中
	color.ba = frac(color.rg * 256);
	// (r,g) 存储的是位数比较高的部分
	color.rg -= color.ba / 256.0f;
	return color;
}

fixed DecodeHeightmap(fixed4 heightmap)
{
	fixed4 table = fixed4(1.0f, -1.0f, 1.0f / 256.0f, -1.0f / 256.0f);
	return dot(heightmap, table);
}

#endif

