# PersonalAnalytics for Researchers

This project was created by and for researchers who want to ask study participants to run PersonalAnalytics on their device to non-intrusively collect computer interaction data in a privacy-protected way. As often times, having access to only automatically collected data is often not sufficient, PersonalAnalytics also offers an experience sampling component, which allows researchers to ask users to reflect and self-report on one or several questions (e.g. Have I been productive? Am I stressed right now?) at customizable times and using Likert-scales. As all collected data is only stored locally on participants' computers, there is an export component, guiding the participant through sharing and potentially obfuscating the captured data, before sharing it with the researchers through their data transfer service of choice. Most settings are configurable in the study-config, everything else can be customized in code.


# Customizing PersonalAnalytics
To customize PersonalAnalytics for your research study, please consider the following steps:

<!-- tbd: Sebastian: update -->

1. Fork the project to work in your own repository.
2. Update the `study.config.ts`-[file](../src/electron/shared/study.config.ts) with your custom study-related settings. Hereby, you can add your custom study name, study title, privacy policy, export upload url as well as contact data. In addition, you can customize which computer interaction tracker isrunning, and if you want to prompt the user to self-report on one or several questions in the experience sampling component.
3. (optional) If you require further customizations, you can create them in the code (see [Contributions Guide](#contributions-guide)).  
4. Use Github Actions (see [build.yml](https://github.com/HASEL-UZH/PersonalAnalytics/blob/feature/electron/.github/workflows/build.yml)) to build and deploy PersonalAnalytics and allow your participants to use it. Using the method, PersonalAnalytics can automatically update your participants' installations with new releases


# Contributions Guide
Anyone is welcome to contribute to PersonalAnalytics by extending it with new trackers or improving existing ones.

This quick guide helps you to set-up your development environment:
<!-- tbd: Sebastian -->


# Research that used PersonalAnalytics
PersonalAnalytics-legacy was used in the following peer-reviewed research projects (and other non-peer reviewed projects too, such as master and bachelor theses):
- [CHI'20](https://andre-meyer.ch/CHI20) Supporting Software Developers’ Focused Work on Window-Based Desktops. Jan Pilzer, Raphael Rosenast. André Meyer. Elaine Huang. Thomas Fritz.
- [TSE'20](https://andre-meyer.ch/TSE20) Detecting Developers’ Task Switches and Types. André Meyer, Chris Satterfield, Manuela Züger, Katja Kevic, Gail Murphy, Thomas Zimmermann, and Thomas Fritz.
- [CSCW’18](https://www.andre-meyer.ch/CSCW18) Design Recommendations for Self-Monitoring in the Workplace: Studies in Software Development. André Meyer, Gail Murphy, Thomas Zimmermann, Thomas Fritz. (hint: in this paper, the tool described as WorkAnalytics refers to the PersonalAnalytics in this repository)
- [CHI’18](http://www.zora.uzh.ch/id/eprint/151128/1/pn4597-zugerA.pdf) Sensing Interruptibility in the Office: A Field Study on the Use of Biometric and Computer Interaction Sensors. Manuela Züger, Sebastian Müller, André Meyer, Thomas Fritz. 
- [TSE’17](https://www.andre-meyer.ch/TSE17) The Work Life of Developers: Activities, Switches and Perceived Productivity. André Meyer, Gail Murphy, Thomas Zimmermann, Laura Barton, Thomas Fritz. 
- [CHI’17](https://www.andre-meyer.ch/CHI17) Reducing Interruptions at Work: A Large-Scale Field Study of FlowLight. Manuela Züger, Christopher Corley, André Meyer, Boyang Li, Thomas Fritz, David Shepherd, Vinay Augustine, Patrick Francis, Nicholas Kraft and Will Snipes.
