#version 300 es
precision highp float;

in vec3 FragPos;
in vec3 Normal;

// Позиция света
uniform float lightPosR;
uniform float lightPosG;
uniform float lightPosB;

// Цвет света
uniform float lightColorR;
uniform float lightColorG;
uniform float lightColorB;

// Цвет объекта
uniform float objectColorR;
uniform float objectColorG;
uniform float objectColorB;

out vec4 FragColor;

void main()
{
    vec3 lightPos = vec3(lightPosR, lightPosG, lightPosB);
    vec3 lightColor = vec3(lightColorR, lightColorG, lightColorB);
    vec3 objectColor = vec3(objectColorR, objectColorG, objectColorB);

    // Ambient
    float ambientStrength = 0.2;
    vec3 ambient = ambientStrength * lightColor;

    // Diffuse
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse) * objectColor;
    FragColor = vec4(result, 1.0);
}