type Commands = {
  createExperienceSample: (promptedAt: number, question: string, response: number) => Promise<void>;
  closeExperienceSamplingWindow: () => Promise<void>;
};
export default Commands;
