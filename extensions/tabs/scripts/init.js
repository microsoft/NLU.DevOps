/* global VSS, renderConfusionMatrix */

VSS.init({
  usePlatformScripts: true,
  taskRestClientLoaderConfig: {
    paths: {
      enhancer: 'scripts'
    }
  }
})

VSS.ready(() => {
  const context = VSS.getWebContext()
  VSS.getConfiguration().onBuildChanged(build => {
    VSS.require(['TFS/DistributedTask/TaskRestClient', 'VSS/Authentication/Services'], (taskRestClient, authServices) => {
      taskRestClient.getClient().getPlanAttachments(context.project.id, 'build', build.orchestrationPlan.planId, 'nlu.devops').then(attachments => {
        let metadata
        let statistics
        attachments.forEach(attachment => {
          if ((attachment.name === 'metadata' || attachment.name === 'statistics') && attachment._links && attachment._links.self && attachment._links.self.href) {
            metadata = attachment.name === 'metadata' ? attachment._links.self.href : metadata
            statistics = attachment.name === 'statistics' ? attachment._links.self.href : statistics
          }
        })

        if (!metadata) {
          console.warn('Could not find attachment for NLU metadata.')
        } else {
          VSS.getAccessToken().then(token => {
            var authToken = authServices.authTokenManager.getAuthorizationHeader(token)
            renderConfusionMatrix(metadata, { Authorization: authToken })
          })
        }

        if (!statistics) {
          console.warn('Could not find attachment for NLU statistics.')
        }
      })
    })
  })
})
