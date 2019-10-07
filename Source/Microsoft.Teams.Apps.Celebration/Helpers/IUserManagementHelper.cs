// <copyright file="IUserManagementHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.Celebration.Models;

    /// <summary>
    /// Stores methods to perform the crud operation in document DB
    /// </summary>
    public interface IUserManagementHelper
    {
        /// <summary>
        /// Get TeamDetails by team Id
        /// </summary>
        /// <param name="teamId">TeamId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<Team> GetTeamsDetailsByTeamIdAsync(string teamId);

        /// <summary>
        /// Get TeamDetails by list of team Ids
        /// </summary>
        /// <param name="teamIds">List of TeamId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<IList<Team>> GetTeamsDetailsByTeamIdsAsync(List<string> teamIds);

        /// <summary>
        /// Save Team Details
        /// </summary>
        /// <param name="team">Team instance</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task SaveTeamDetailsAsync(Team team);

        /// <summary>
        /// Delete Team Detail
        /// </summary>
        /// <param name="teamId">TeamId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteTeamDetailsAsync(string teamId);

        /// <summary>
        /// Get User by aadObjectId
        /// </summary>
        /// <param name="aadObjectId">AadObjectId of user</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<Models.User> GetUserByAadObjectIdAsync(string aadObjectId);

        /// <summary>
        /// Add or update user
        /// </summary>
        /// <param name="user">User object</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task SaveUserAsync(Models.User user);

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="userTeamsId">User teams Id</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteUserByTeamsIdAsync(string userTeamsId);

        /// <summary>
        /// Add record in UserTeamMembership collection
        /// </summary>
        /// <param name="userTeamMembership">UserTeamMembership object</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task AddUserTeamMembershipAsync(UserTeamMembership userTeamMembership);

        /// <summary>
        /// Delete UserTeamMembership record
        /// </summary>
        /// <param name="userTeamsId">User's teamsId</param>
        /// <param name="teamId">TeamId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteUserTeamMembershipAsync(string userTeamsId, string teamId);

        /// <summary>
        /// Delete UserTeamMembership record
        /// </summary>
        /// <param name="teamId">TeamId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteUserTeamMembershipByTeamIdAsync(string teamId);

        /// <summary>
        /// Returns UserTeamMembership list
        /// </summary>
        /// <param name="teamId">TeamId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<IList<UserTeamMembership>> GetUserTeamMembershipByTeamIdAsync(string teamId);

        /// <summary>
        /// Returns UserTeamMembership list
        /// </summary>
        /// <param name="userTeamsId">User's teamId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<IList<UserTeamMembership>> GetUserTeamMembershipByTeamsIdAsync(string userTeamsId);
    }
}