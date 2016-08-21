#ifndef HEIGHTCODE_INCLUDED
#define HEIGHTCODE_INCLUDED

//�Ѹ߶ȷֽ��(RGBA)4����ֵ
fixed4 EncodeHeightmap(fixed fHeight)
{
	fixed h = fHeight;
	fixed positive = fHeight > 0 ? fHeight : 0;
	fixed negative = fHeight < 0 ? -fHeight : 0;

	fixed4 color = 0;

	color.r = positive;
	color.g = negative;

	//��С�����бȽϵ͵�λ������(b,a)��
	color.ba = frac(color.rg * 256);
	// (r,g) �洢����λ���ȽϸߵĲ���
	color.rg -= color.ba / 256.0f;
	return color;
}

fixed DecodeHeightmap(fixed4 heightmap)
{
	fixed4 table = fixed4(1.0f, -1.0f, 1.0f / 256.0f, -1.0f / 256.0f);
	return dot(heightmap, table);
}

#endif

