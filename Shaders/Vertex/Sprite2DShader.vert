#version 330 core

layout (location = 0) in vec3 aPosition;

layout (location = 1) in vec2 aTexCoord;

layout (location = 2) in vec4 aColor;

out vec2 texCoord;

out vec4 color;

uniform mat4 aProjection;

uniform mat4 aView;

uniform mat4 aModel;

void main()
{
    gl_Position = vec4(aPosition, 1.0) * aModel * aView * aProjection;

    texCoord = aTexCoord;

    color = aColor;
}