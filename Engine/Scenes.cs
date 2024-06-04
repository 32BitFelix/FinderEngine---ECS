namespace FinderEngine.Scenes;


// Signals what object shall be
// classified as a scene
public class SceneAttribute : Attribute;

// Signals that the scene or system
// with this attribute shall begin
// running upon start of the application
public class StarterAttribute : Attribute;