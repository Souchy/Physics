shader_type canvas_item;
uniform float animation_speed = 1.0;
uniform float range_x = 1.0;
uniform float range_y = 1.0;

uniform vec4 border_color : source_color = vec4(1.0);
uniform float width : hint_range(0, 10) = 1.0;
uniform int pattern : hint_range(0, 2) = 0; // diamond, circle, square
uniform bool inside = false;
uniform bool add_margins = true; // only useful when inside is false

void vertex() {
	// Called for every vertex the material is visible on.
	
	float scale_x = range_x * 0.5 * (range_x + abs(sin(TIME * animation_speed)));
	float scale_y = range_y * 0.5 * (range_y + abs(cos(TIME * animation_speed)));
	
	if (add_margins) {
		VERTEX += (UV * 2.0 - 1.0) * width;
	}
	
	VERTEX.x *= scale_x;
	VERTEX.y *= scale_y;
}

bool hasContraryNeighbour(vec2 uv, vec2 texture_pixel_size, sampler2D texture) {
	for (float i = -ceil(width); i <= ceil(width); i++) {
		float x = abs(i) > width ? width * sign(i) : i;
		float offset;
		
		if (pattern == 0) {
			offset = width - abs(x);
		} else if (pattern == 1) {
			offset = floor(sqrt(pow(width + 0.5, 2) - x * x));
		} else if (pattern == 2) {
			offset = width;
		}
		
		for (float j = -ceil(offset); j <= ceil(offset); j++) {
			float y = abs(j) > offset ? offset * sign(j) : j;
			vec2 xy = uv + texture_pixel_size * vec2(x, y);
			
			if ((xy != clamp(xy, vec2(0.0), vec2(1.0)) || texture(texture, xy).a <= 0.0) == inside) {
				return true;
			}
		}
	}
	return false;
}

void fragment() {
	// Called for every pixel the material is visible on.
	//COLOR.r = 1.0;
	vec2 uv = UV;
	if (add_margins) {
		vec2 texture_pixel_size = vec2(1.0) / (vec2(1.0) / TEXTURE_PIXEL_SIZE + vec2(width * 2.0));
		uv = (uv - texture_pixel_size * width) * TEXTURE_PIXEL_SIZE / texture_pixel_size;
		if (uv != clamp(uv, vec2(0.0), vec2(1.0))) {
			COLOR.a = 0.0;
		} else {
			//COLOR = texture(TEXTURE, uv);
		}
	} else {
		//COLOR = texture(TEXTURE, uv);
	}
	
	if ((COLOR.a > 0.0) == inside && hasContraryNeighbour(uv, TEXTURE_PIXEL_SIZE, TEXTURE)) {
		COLOR.rgb = inside ? mix(COLOR.rgb, border_color.rgb, border_color.a) : border_color.rgb;
		COLOR.a += (1.0 - COLOR.a) * border_color.a;
	}
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
