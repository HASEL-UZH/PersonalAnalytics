type Commands = {
  createExperienceSample: (promptedAt: number, question: string, response: number) => Promise<void>;
};
export default Commands;
