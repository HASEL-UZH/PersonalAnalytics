type Commands = {
  createExperienceSample: (promptedAt: number, question: string, response: string) => Promise<void>;
};
export default Commands;
