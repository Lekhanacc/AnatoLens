# AnatoLens project presentation summary

## Deliverables
- PowerPoint: `AnatoLens_Project_Presentation.pptx` (22 slides, 16:9, editable PowerPoint shapes and text).
- PDF: not generated; no PowerPoint or LibreOffice PDF exporter is available in this environment.
- Speaker notes: included on all 22 slides with talking points, transition, and suggested duration.
- Screenshots and supporting assets: `AnatoLens_Presentation_Assets/`.
- Four editable/source SVG diagrams are included under `AnatoLens_Presentation_Assets/Diagrams/`.

## Slide titles
1. ANATOLENS — Interactive 3D Anatomy Learning Through Augmented Reality
2. A scan turns a reference into an interactive anatomy view
3. The learning challenge: anatomy is spatial, while many study views are flat
4. From a student idea to a buildable set of objectives
5. The user journey: scan, inspect, and learn
6. System architecture: a local Unity app with image-target mapping
7. Technology stack verified in project configuration
8. Development process: integrate one layer at a time
9. Image tracking is predefined and deterministic
10. Model catalog: 11 registered targets in the active scene
11. Model controls keep exploration direct
12. Numbered labels are available on selected models
13. Organ explanations are local content with learning links
14. AI chat and voice are integrated, but service-dependent
15. Onboarding and AR interface guide the learner
16. Suggested live demo: follow the model from target to learning
17. Implementation highlights: reusable scene components
18. Verification status: code evidence is not the same as device testing
19. Actual application output: selected anatomy views
20. Engineering challenges recorded in the project
21. Current boundaries define the next development steps
22. ANATOLENS — See Beyond the Books / Thank You

## Actual screenshots used
- Heart output photo; eye output photo; kidney output with menu; female reproductive system with numbered labels; kidney explanation panel.
- Supplied AnatoLens splash artwork is used as artwork, explicitly not presented as a live app capture.
- Original attachments are copied into `AnatoLens_Presentation_Assets/Screenshots/`; project source assets were not overwritten.

## Features verified in project files
- Unity 2022.3.62f3; Android / ARCore target; AR Foundation and ARCore 5.2.2; embedded glTFast 6.14.1.
- Active build scene: `Assets/Scenes/SampleScene.unity`; reference library: `Assets/MyImages.asset`.
- 11 registered targets and explicit mappings for Heart, Brain, Eye, Lung, Kidney, Neuron, Female reproductive system, Digestive system, Plant cell, Nephron, and Animal cell.
- Touch rotation, pinch zoom, reset, fix/unlock; onboarding; contextual labels (Heart, Brain, Neuron, Female reproductive); local organ explanation content.
- AI chat, OpenAI organ context, microphone recording, Whisper transcription code paths.

## Scope and evaluation notes
- The presentation describes the configured target-to-prefab mappings and learning interfaces present in the project. Labels and AI/voice are presented as application features, not as a statement that every topic or service has a separate completion status.
- No FPS, tracking accuracy, response latency, user study, or learning outcome values were found in the repository; the presentation does not claim numerical results.
- The Digestive System target currently maps to a stomach model asset, rather than a complete digestive tract model.
- Android device measurements should be collected during the live demonstration and documented as observed results.

## Information to complete manually
- Rehearse the live demo on the presentation Android device and confirm target print dimensions.
- Export to PDF from PowerPoint if required.

## Assumptions and duration
- Speaker timing totals approximately 14 minutes excluding demo and Q&A.
- Supplied phone/display photos are treated as authentic application output, with their photographic context retained.
- “Integrated” means project configuration/code evidence, not a claim that every feature passed device testing.
