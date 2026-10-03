using System.Collections.Generic;
using JobSequenceApp.Models;
using JobSequenceApp.Models.ViewModels;

namespace JobSequenceApp.Services
{
    public interface IJobSequencingService
    {
        JobSequenceResultViewModel CalculateSequence(List<Job> jobs);
    }
}
