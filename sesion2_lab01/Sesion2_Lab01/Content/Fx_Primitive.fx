﻿struct VS_IN {
	float4 pos : POSITION0;
	float4 col : COLOR;
	float4 velocity : POSITION1;
};

struct PS_IN {
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

PS_IN VS( VS_IN input ) {
	PS_IN output = (PS_IN)0;
	
	output.pos = input.pos;
	output.col = input.col;

	output.pos.x *= sin(input.velocity.x);
	output.pos.y *= sin(input.velocity.y);
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target {
	return input.col;
}

technique10 Render {
	pass P0 {
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}