when the default schedule is changed, including changing module time and adding/changing breaks, ALL schemas are deleted. someone is bound to do this by accident at some point. we should have a restore/undo functionality. we can dump all schemas as json in s3 and restore them from there if needed.
we also need to style the prompt that warns the user so it seems less generic.
