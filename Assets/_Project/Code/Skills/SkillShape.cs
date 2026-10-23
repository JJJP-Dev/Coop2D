// The area a skill hits (#162). Saved in the skill assets as a number: add new shapes at the end.
public enum SkillShape
{
    // A circle where you aim, up to the cast range: Estallido, Marca, Llovizna, Cortocircuito.
    Circle,

    // A straight band from the caster towards the cursor that hits everything in it: Chispa.
    Line,

    // A wedge from the caster towards the cursor: Ola.
    Cone,

    // Something that flies from the caster towards the cursor and hits on the way: Llamarada, Burbuja,
    // Rayo. Its path is a line as wide as the projectile; where it bursts, a circle.
    Projectile,
}
